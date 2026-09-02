using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Codes;
using DividendHarvest.Domain.Portfolio;
using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.DividendStrategy;

public sealed class PortfolioRecommendationAppService(
    IUow uow,
    IStockWatchlistAppService stockWatchlistAppService,
    IStockAnalysisAppService stockAnalysisAppService,
    IBudgetAppService budgetAppService,
    TimeProvider timeProvider) : IPortfolioRecommendationAppService
{
    public async Task<PortfolioRecommendationResult> GetAsync(
        CancellationToken cancellationToken)
    {
        var watchlist = await stockWatchlistAppService.GetAsync(cancellationToken);
        var analyses = new List<StockAnalysisResult>(watchlist.Count);
        foreach (var stock in watchlist)
        {
            analyses.Add(await stockAnalysisAppService.GetAsync(
                new GetStockAnalysisRequest(stock.SecurityCode, stock.ExchangeCode),
                cancellationToken));
        }

        var budgetSummary = await budgetAppService.GetSummaryAsync(cancellationToken);
        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var parameters = await uow.Get<ModelParameterSet>()
            .GetQueryable(asNoTracking: true)
            .Where(parameter =>
                parameter.PortfolioId == budgetSummary.PortfolioId
                && parameter.EffectiveFromDate <= currentDate)
            .ToListAsync(cancellationToken);
        var parametersById = parameters.ToDictionary(parameter => parameter.Id);
        var totalPortfolioValue = analyses
            .Where(analysis => analysis.ClosePrice is not null)
            .Sum(analysis => analysis.HeldShares * analysis.ClosePrice!.Value);
        var portfolioValuationComplete = analyses
            .All(analysis => analysis.HeldShares <= 0 || analysis.ClosePrice is not null);
        var sectorMarketValues = Enumerable.Range(0, analyses.Count)
            .Where(index => !string.IsNullOrWhiteSpace(watchlist[index].SectorCode))
            .GroupBy(index => watchlist[index].SectorCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(index =>
                    analyses[index].HeldShares
                    * (analyses[index].ClosePrice ?? 0m)),
                StringComparer.OrdinalIgnoreCase);
        var cashReserveRatio = PortfolioBudgetCalculator.CalculateCurrentCashReserveRatio(
            parameters,
            currentDate);
        var startingAvailableBudget = portfolioValuationComplete
            ? PortfolioBudgetCalculator.CalculateAvailableBudget(
                budgetSummary.CashBalanceAmount,
                totalPortfolioValue,
                cashReserveRatio)
            : 0m;
        var remainingBudget = startingAvailableBudget;
        var totalSuggestedTradeAmount = 0m;
        var totalTransactionFeeAmount = 0m;
        var adjustedAnalyses = analyses.ToArray();

        var orderedIndexes = Enumerable.Range(0, analyses.Count)
            .OrderBy(index => GetPricePriority(analyses[index].PriceZoneCode))
            .ThenBy(index =>
                analyses[index].DividendReliabilityCode == DividendReliabilityCodes.Passed ? 0 : 1)
            .ThenByDescending(index => GetTargetGap(watchlist[index].Holding))
            .ThenBy(index => index)
            .ToArray();

        foreach (var index in orderedIndexes)
        {
            var analysis = analyses[index];
            if (analysis.ModelParameterSetId is not { } parameterId
                || !parametersById.TryGetValue(parameterId, out var parameter)
                || analysis.ClosePrice is not { } closePrice
                || analysis.PriceZoneCode is not { } priceZoneCode)
            {
                continue;
            }

            var trade = TradeQuantityCalculator.Calculate(
                parameter,
                analysis.ModelStatusCode,
                analysis.DividendReliabilityCode,
                priceZoneCode,
                closePrice,
                analysis.HeldShares,
                analysis.CoreShares,
                watchlist[index].Holding?.TargetShares ?? 0,
                remainingBudget,
                totalPortfolioValue > 0 ? totalPortfolioValue : null,
                analysis.HeldShares * closePrice,
                watchlist[index].SectorCode is { } sectorCode
                    && sectorMarketValues.TryGetValue(sectorCode, out var sectorMarketValue)
                    ? sectorMarketValue
                    : null);
            var adjustedAnalysis = analysis with
            {
                SuggestedBuyShares = trade.SuggestedBuyShares,
                SuggestedSellShares = trade.SuggestedSellShares,
                SuggestedTradeAmount = trade.SuggestedTradeAmount,
                EstimatedTransactionFeeAmount = trade.EstimatedTransactionFeeAmount
            };
            if (trade.SuggestedBuyShares > 0)
            {
                remainingBudget = Math.Max(
                    remainingBudget
                        - trade.SuggestedTradeAmount
                        - trade.EstimatedTransactionFeeAmount,
                    0m);
            }
            else if (IsBuyZone(priceZoneCode)
                && analysis.ModelStatusCode == ModelStatusCodes.Available
                && analysis.DividendReliabilityCode == DividendReliabilityCodes.Passed)
            {
                adjustedAnalysis = adjustedAnalysis with
                {
                    Explanation = !portfolioValuationComplete
                        ? $"{analysis.Explanation} 组合中存在缺少有效收盘价的持仓，本期不生成买入建议。"
                        : $"{analysis.Explanation} 本期组合预算或仓位额度不足，建议股数为 0。"
                };
            }

            totalSuggestedTradeAmount += trade.SuggestedTradeAmount;
            totalTransactionFeeAmount += trade.EstimatedTransactionFeeAmount;
            adjustedAnalyses[index] = adjustedAnalysis;
        }

        return new PortfolioRecommendationResult(
            budgetSummary.PortfolioId,
            startingAvailableBudget,
            remainingBudget,
            totalSuggestedTradeAmount,
            totalTransactionFeeAmount,
            adjustedAnalyses,
            timeProvider.GetUtcNow());
    }

    private static int GetPricePriority(string? priceZoneCode)
        => priceZoneCode switch
        {
            PriceZoneCodes.StrongBuy => 0,
            PriceZoneCodes.Accumulate => 1,
            _ => 2
        };

    private static int GetTargetGap(StockHoldingSnapshot? holding)
        => holding is null
            ? 0
            : Math.Max(holding.TargetShares - holding.HeldShares, 0);

    private static bool IsBuyZone(string priceZoneCode)
        => priceZoneCode is PriceZoneCodes.StrongBuy or PriceZoneCodes.Accumulate;
}
