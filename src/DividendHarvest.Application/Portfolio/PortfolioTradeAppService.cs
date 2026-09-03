using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Exceptions;
using DividendHarvest.Domain.Models;
using PortfolioEntity = DividendHarvest.Domain.Models.Portfolio;
using DividendHarvest.Domain.Portfolio;
using DividendHarvest.Domain.Securities;
using FluentValidation;

namespace DividendHarvest.Application.Portfolio;

public sealed class PortfolioTradeAppService(
    IUow uow,
    IValidator<RecordPortfolioTradeRequest> requestValidator) : IPortfolioTradeAppService
{
    public async Task<PortfolioTradeResult> RecordAsync(
        RecordPortfolioTradeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationResult = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw ApplicationErrors.Validation(
                ApplicationErrorCodes.PortfolioTradeValidationFailed,
                ValidationErrorFormatter.Format(validationResult));
        }

        var portfolio = await uow.Get<PortfolioEntity>()
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);
        if (portfolio is null)
        {
            throw ApplicationErrors.Simple(ApplicationErrorCodes.SetupNotCompleted);
        }

        var reference = AShareReference.Create(request.SecurityCode, request.ExchangeCode);
        var security = await uow.Get<Security>()
            .SingleOrDefaultAsync(
                item =>
                    item.SecurityCode == reference.SecurityCode
                    && item.ExchangeCode == reference.ExchangeCode,
                cancellationToken);
        if (security is null)
        {
            throw ApplicationErrors.WithSecurityReference(
                ApplicationErrorCodes.StockNotConfigured,
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        var tradeRepository = uow.Get<PortfolioTrade>();
        var sourceRecordId = request.SourceRecordId?.Trim();
        var existingTrade = string.IsNullOrWhiteSpace(sourceRecordId)
            ? null
            : await tradeRepository
                .SingleOrDefaultAsync(
                    item => item.PortfolioId == portfolio.Id
                        && item.SourceRecordId == sourceRecordId,
                    cancellationToken);
        var positionRepository = uow.Get<PortfolioPosition>();
        var position = await positionRepository
            .SingleOrDefaultAsync(
                item => item.PortfolioId == portfolio.Id
                    && item.SecurityId == security.Id,
                cancellationToken,
                asNoTracking: false);
        if (existingTrade is not null)
        {
            if (!MatchesRequest(existingTrade, request, security.Id))
            {
                throw ApplicationErrors.WithSourceRecord(
                    ApplicationErrorCodes.PortfolioTradeConflict,
                    sourceRecordId!);
            }

            if (position is null)
            {
                throw ApplicationErrors.Simple(
                    ApplicationErrorCodes.PortfolioPositionMissingForTrade);
            }

            return ToResult(existingTrade, portfolio, reference, position);
        }

        PortfolioTrade trade;
        try
        {
            trade = PortfolioTrade.Create(
                portfolio.Id,
                security.Id,
                request.TradeDate,
                request.TradeDirectionCode,
                request.ShareQuantity,
                request.PricePerShare,
                request.TransactionFeeAmount,
                request.SourceRecordId);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationErrors.Validation(
                ApplicationErrorCodes.PortfolioTradeValidationFailed,
                exception.Message);
        }

        try
        {
            if (trade.TradeDirectionCode == TradeDirectionCodes.Buy)
            {
                if (position is null)
                {
                    position = new PortfolioPosition
                    {
                        PortfolioId = portfolio.Id,
                        SecurityId = security.Id,
                        HeldShares = 0,
                        CoreShares = 0,
                        TargetShares = 0,
                        AverageCostPerShare = 0m
                    };
                    await positionRepository.AddAsync(position, cancellationToken);
                }

                position.ApplyBuy(
                    trade.ShareQuantity,
                    trade.PricePerShare,
                    trade.TransactionFeeAmount);
            }
            else if (position is null)
            {
                throw ApplicationErrors.Simple(
                    ApplicationErrorCodes.PortfolioPositionMissingForTrade);
            }
            else
            {
                position.ApplySell(trade.ShareQuantity);
            }
        }
        catch (InvalidOperationException exception)
        {
            throw ApplicationErrors.Validation(
                ApplicationErrorCodes.PortfolioTradeValidationFailed,
                exception.Message);
        }

        await tradeRepository.AddAsync(trade, cancellationToken);
        var cashLedgerRepository = uow.Get<CashLedgerEntry>();
        foreach (var cashEntry in PortfolioTradeCashLedger.CreateEntries(trade))
        {
            await cashLedgerRepository.AddAsync(cashEntry, cancellationToken);
        }

        try
        {
            await uow.CommitAsync(cancellationToken);
        }
        catch (UnitOfWorkCommitException exception)
            when (!string.IsNullOrWhiteSpace(sourceRecordId)
                && exception.IsUniqueConstraintViolation)
        {
            // The filtered unique index protects against concurrent duplicate
            // submissions that both pass the read-before-insert check.
            throw ApplicationErrors.WithSourceRecord(
                ApplicationErrorCodes.PortfolioTradeConflict,
                sourceRecordId);
        }

        return ToResult(trade, portfolio, reference, position);
    }

    private static PortfolioTradeResult ToResult(
        PortfolioTrade trade,
        PortfolioEntity portfolio,
        AShareReference reference,
        PortfolioPosition position)
        => new(
            trade.Id,
            portfolio.Id,
            reference.SecurityCode,
            reference.ExchangeCode,
            trade.TradeDate,
            trade.TradeDirectionCode,
            trade.ShareQuantity,
            trade.PricePerShare,
            trade.TransactionFeeAmount,
            position.HeldShares,
            position.CoreShares,
            position.TargetShares,
            position.AverageCostPerShare,
            trade.ShareQuantity * trade.PricePerShare);

    private static bool MatchesRequest(
        PortfolioTrade trade,
        RecordPortfolioTradeRequest request,
        Guid securityId)
        => trade.SecurityId == securityId
            && trade.TradeDate == request.TradeDate
            && trade.TradeDirectionCode == NormalizeCode(request.TradeDirectionCode)
            && trade.ShareQuantity == request.ShareQuantity
            && trade.PricePerShare == request.PricePerShare
            && trade.TransactionFeeAmount == request.TransactionFeeAmount;

    private static string NormalizeCode(string value)
        => value.Trim().ToLowerInvariant();
}
