using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Analysis;

public sealed class StockAnalysisAppService(
    IUow uow,
    IValidator<GetStockAnalysisRequest> requestValidator,
    TimeProvider timeProvider) : IStockAnalysisAppService
{
    public async Task<StockAnalysisResult> GetAsync(
        GetStockAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationResult = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new StockAnalysisValidationException(
                ValidationErrorFormatter.Format(validationResult));
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

        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var parameters = await uow.Get<ModelParameterSet>()
            .GetQueryable(asNoTracking: true)
            .Where(parameter =>
                parameter.SecurityId == security.Id
                && parameter.EffectiveFromDate <= currentDate)
            .OrderByDescending(parameter => parameter.EffectiveFromDate)
            .FirstOrDefaultAsync(cancellationToken);
        var priceObservation = await uow.Get<PriceObservation>()
            .GetQueryable(asNoTracking: true)
            .Where(observation =>
                observation.SecurityId == security.Id
                && observation.TradingDate <= currentDate)
            .OrderByDescending(observation => observation.TradingDate)
            .FirstOrDefaultAsync(cancellationToken);
        var dividendEvents = await uow.Get<DividendEvent>()
            .GetQueryable(asNoTracking: true)
            .Where(dividendEvent => dividendEvent.SecurityId == security.Id)
            .ToListAsync(cancellationToken);
        var financialSnapshots = await uow.Get<FinancialSnapshot>()
            .GetQueryable(asNoTracking: true)
            .Where(snapshot => snapshot.SecurityId == security.Id)
            .ToListAsync(cancellationToken);
        var position = await uow.Get<PortfolioPosition>()
            .GetQueryable(asNoTracking: true)
            .FirstOrDefaultAsync(
                currentPosition => currentPosition.SecurityId == security.Id
                    && (parameters == null
                        || currentPosition.PortfolioId == parameters.PortfolioId),
                cancellationToken);

        var heldShares = position?.HeldShares ?? 0;
        var coreShares = position?.CoreShares ?? 0;
        var satelliteShares = Math.Max(heldShares - coreShares, 0);
        var modelDividendPerShare = priceObservation is null
            ? null
            : TtmDividendCalculator.Calculate(
                dividendEvents,
                priceObservation.TradingDate);
        var reliabilityCode = modelDividendPerShare is null
            ? "unavailable"
            : DividendReliabilityEvaluator.Evaluate(
                dividendEvents,
                financialSnapshots,
                priceObservation!.TradingDate);
        var computedAt = timeProvider.GetUtcNow();

        if (parameters is null
            || priceObservation is null
            || modelDividendPerShare is null)
        {
            return CreateUnavailableResult(
                security,
                reference,
                priceObservation,
                modelDividendPerShare,
                reliabilityCode,
                heldShares,
                coreShares,
                satelliteShares,
                computedAt);
        }

        var cashEntries = await uow.Get<CashLedgerEntry>()
            .GetQueryable(asNoTracking: true)
            .Where(entry => entry.PortfolioId == parameters.PortfolioId)
            .ToListAsync(cancellationToken);
        var ledgerBalance = cashEntries
            .Where(entry => entry.CashDirectionCode == "inflow")
            .Sum(entry => entry.CashAmount)
            - cashEntries
                .Where(entry => entry.CashDirectionCode == "outflow")
                .Sum(entry => entry.CashAmount);
        var portfolioPositions = await uow.Get<PortfolioPosition>()
            .GetQueryable(asNoTracking: true)
            .Where(currentPosition => currentPosition.PortfolioId == parameters.PortfolioId)
            .ToListAsync(cancellationToken);
        var portfolioPriceObservations = await uow.Get<PriceObservation>()
            .GetQueryable(asNoTracking: true)
            .Where(observation => observation.TradingDate <= currentDate)
            .ToListAsync(cancellationToken);
        var latestPrices = portfolioPriceObservations
            .GroupBy(observation => observation.SecurityId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(observation => observation.TradingDate)
                    .First());
        var totalPortfolioValue = portfolioPositions
            .Where(currentPosition => latestPrices.ContainsKey(currentPosition.SecurityId))
            .Sum(currentPosition =>
                currentPosition.HeldShares
                * latestPrices[currentPosition.SecurityId].ClosePrice);
        var availableBudgetAmount = Math.Max(
            ledgerBalance
                - totalPortfolioValue * parameters.CashReserveRatio,
            0m);
        var modelStatusCode = reliabilityCode switch
        {
            "passed" => "available",
            "failed" => "failed",
            "re_evaluate" => "re_evaluate",
            _ => "cautious"
        };
        var priceZone = DividendPriceZoneCalculator.Calculate(
            parameters,
            modelDividendPerShare.Value,
            priceObservation.ClosePrice);
        var recommendationCode = reliabilityCode == "passed"
            ? priceZone.PriceZoneCode
            : "no_action";
        var tradeQuantity = TradeQuantityCalculator.Calculate(
            parameters,
            modelStatusCode,
            reliabilityCode,
            priceZone.PriceZoneCode,
            priceObservation.ClosePrice,
            heldShares,
            coreShares,
            position?.TargetShares ?? 0,
            availableBudgetAmount,
            totalPortfolioValue > 0 ? totalPortfolioValue : null,
            position is null
                ? 0m
                : position.HeldShares * priceObservation.ClosePrice,
            null);
        var explanation = reliabilityCode == "passed"
            ? "股息可靠性检查通过，当前价格区域可用于生成后续预算建议。"
            : "TTM 股息率和价格区域已计算，但股息可靠性资料尚未完整，当前只提供谨慎参考。";

        return new StockAnalysisResult(
            reference.SecurityCode,
            reference.ExchangeCode,
            security.SecurityName,
            modelStatusCode,
            reliabilityCode,
            priceObservation.ClosePrice,
            modelDividendPerShare,
            "ttm",
            priceZone.DividendYield,
            priceZone.StrongBuyPrice,
            priceZone.AccumulatePrice,
            priceZone.PartialTrimPrice,
            priceZone.AggressiveTrimPrice,
            priceZone.PriceZoneCode,
            recommendationCode,
            heldShares,
            coreShares,
            satelliteShares,
            tradeQuantity.SuggestedBuyShares,
            tradeQuantity.SuggestedSellShares,
            tradeQuantity.SuggestedTradeAmount,
            tradeQuantity.EstimatedTransactionFeeAmount,
            priceObservation.TradingDate,
            parameters.Id,
            computedAt,
            explanation);
    }

    private static StockAnalysisResult CreateUnavailableResult(
        Security security,
        AShareReference reference,
        PriceObservation? priceObservation,
        decimal? modelDividendPerShare,
        string reliabilityCode,
        int heldShares,
        int coreShares,
        int satelliteShares,
        DateTimeOffset computedAt)
        => new(
            reference.SecurityCode,
            reference.ExchangeCode,
            security.SecurityName,
            "unavailable",
            reliabilityCode,
            priceObservation?.ClosePrice,
            modelDividendPerShare,
            modelDividendPerShare is null ? null : "ttm",
            null,
            null,
            null,
            null,
            null,
            null,
            "no_action",
            heldShares,
            coreShares,
            satelliteShares,
            0,
            0,
            0m,
            0m,
            priceObservation?.TradingDate,
            null,
            computedAt,
            "缺少有效模型参数、行情或 TTM 实际股息，暂不生成价格区域和交易建议。");
}
