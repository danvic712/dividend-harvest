using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Recommendations;

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
        var parameterIds = analyses
            .Where(analysis => analysis.ModelParameterSetId is not null)
            .Select(analysis => analysis.ModelParameterSetId!.Value)
            .Distinct()
            .ToArray();
        var parameters = parameterIds.Length == 0
            ? []
            : await uow.Get<ModelParameterSet>()
                .GetQueryable(asNoTracking: true)
                .Where(parameter => parameterIds.Contains(parameter.Id))
                .ToListAsync(cancellationToken);
        var parametersById = parameters.ToDictionary(parameter => parameter.Id);
        var totalPortfolioValue = analyses
            .Where(analysis => analysis.ClosePrice is not null)
            .Sum(analysis => analysis.HeldShares * analysis.ClosePrice!.Value);
        var cashReserveRatio = parameters.Count == 0
            ? 0m
            : parameters.Max(parameter => parameter.CashReserveRatio);
        var startingAvailableBudget = Math.Max(
            budgetSummary.AvailableBudgetAmount
                - totalPortfolioValue * cashReserveRatio,
            0m);
        var remainingBudget = startingAvailableBudget;
        var totalSuggestedTradeAmount = 0m;
        var totalTransactionFeeAmount = 0m;
        var adjustedAnalyses = analyses.ToArray();

        var orderedIndexes = Enumerable.Range(0, analyses.Count)
            .OrderBy(index => GetPricePriority(analyses[index].PriceZoneCode))
            .ThenBy(index => analyses[index].DividendReliabilityCode == "passed" ? 0 : 1)
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
                analysis.HeldShares * closePrice);
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
                && analysis.ModelStatusCode == "available"
                && analysis.DividendReliabilityCode == "passed")
            {
                adjustedAnalysis = adjustedAnalysis with
                {
                    Explanation = $"{analysis.Explanation} 本期组合预算或仓位额度不足，建议股数为 0。"
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
            "strong_buy" => 0,
            "accumulate" => 1,
            _ => 2
        };

    private static int GetTargetGap(StockHoldingSnapshot? holding)
        => holding is null
            ? 0
            : Math.Max(holding.TargetShares - holding.HeldShares, 0);

    private static bool IsBuyZone(string priceZoneCode)
        => priceZoneCode is "strong_buy" or "accumulate";
}
