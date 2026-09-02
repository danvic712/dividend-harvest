using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Portfolio;
using DividendHarvest.Domain.Securities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Trades;

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
            throw new PortfolioTradeValidationException(
                ValidationErrorFormatter.Format(validationResult));
        }

        var portfolio = await uow.Get<Portfolio>()
            .GetQueryable(asNoTracking: true)
            .SingleOrDefaultAsync(cancellationToken);
        if (portfolio is null)
        {
            throw new SetupNotCompletedException();
        }

        var reference = AShareReference.Create(request.SecurityCode, request.ExchangeCode);
        var security = await uow.Get<Security>()
            .GetQueryable(asNoTracking: true)
            .SingleOrDefaultAsync(
                item =>
                    item.SecurityCode == reference.SecurityCode
                    && item.ExchangeCode == reference.ExchangeCode,
                cancellationToken);
        if (security is null)
        {
            throw new StockNotConfiguredException(
                reference.SecurityCode,
                reference.ExchangeCode);
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
            throw new PortfolioTradeValidationException(exception.Message);
        }

        var positionRepository = uow.Get<PortfolioPosition>();
        var position = await positionRepository
            .GetQueryable()
            .SingleOrDefaultAsync(
                item => item.PortfolioId == portfolio.Id
                    && item.SecurityId == security.Id,
                cancellationToken);
        try
        {
            if (trade.TradeDirectionCode == "buy")
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
                throw new PortfolioTradeValidationException("卖出交易没有对应的当前持仓。");
            }
            else
            {
                position.ApplySell(trade.ShareQuantity);
            }
        }
        catch (InvalidOperationException exception)
        {
            throw new PortfolioTradeValidationException(exception.Message);
        }

        await uow.Get<PortfolioTrade>().AddAsync(trade, cancellationToken);
        var cashAmount = trade.ShareQuantity * trade.PricePerShare;
        await uow.Get<CashLedgerEntry>().AddAsync(
            CashLedgerEntry.Create(
                portfolio.Id,
                security.Id,
                trade.TradeDate,
                trade.TradeDirectionCode,
                trade.TradeDirectionCode == "buy" ? "outflow" : "inflow",
                cashAmount,
                $"{trade.Id}:trade"),
            cancellationToken);
        if (trade.TransactionFeeAmount > 0)
        {
            await uow.Get<CashLedgerEntry>().AddAsync(
                CashLedgerEntry.Create(
                    portfolio.Id,
                    security.Id,
                    trade.TradeDate,
                    "fee",
                    "outflow",
                    trade.TransactionFeeAmount,
                    $"{trade.Id}:fee"),
                cancellationToken);
        }

        await uow.CommitAsync(cancellationToken);

        return new PortfolioTradeResult(
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
            cashAmount);
    }
}
