using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Codes;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Portfolio;

namespace DividendHarvest.Application.DividendStrategy;

public sealed class PortfolioAllocationAppService(
    IUow uow,
    TimeProvider timeProvider) : IPortfolioAllocationAppService
{
    public async Task<PortfolioRecommendationResult> RunAsync(
        IReadOnlyList<StockWatchlistItem> watchlist,
        IReadOnlyList<StockAnalysisResult> analyses,
        BudgetSummary budgetSummary,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(watchlist);
        ArgumentNullException.ThrowIfNull(analyses);
        ArgumentNullException.ThrowIfNull(budgetSummary);

        if (watchlist.Count != analyses.Count)
        {
            throw new ArgumentException(
                "关注股票和单股分析结果的数量必须一致。",
                nameof(analyses));
        }

        var analysesBySecurityId = analyses.ToDictionary(analysis => analysis.SecurityId);
        var alignedAnalyses = watchlist
            .Select(stock =>
            {
                if (stock.SecurityId == Guid.Empty
                    || !analysesBySecurityId.TryGetValue(stock.SecurityId, out var analysis)
                    || analysis.SecurityCode != stock.SecurityCode
                    || analysis.ExchangeCode != stock.ExchangeCode)
                {
                    throw new ArgumentException(
                        $"股票 {stock.SecurityCode}/{stock.ExchangeCode} 的单股分析身份不匹配。",
                        nameof(analyses));
                }

                return analysis;
            })
            .ToArray();

        var computedAt = timeProvider.GetUtcNow();
        var currentDate = DateOnly.FromDateTime(computedAt.UtcDateTime);
        var parameters = await uow.Get<ModelParameterSet>().ListAsync(
            parameter =>
                parameter.PortfolioId == budgetSummary.PortfolioId
                && parameter.EffectiveFromDate <= currentDate,
            cancellationToken: cancellationToken);
        var parametersById = parameters.ToDictionary(parameter => parameter.Id);
        var totalPortfolioValue = alignedAnalyses
            .Where(analysis => analysis.ClosePrice is not null)
            .Sum(analysis => analysis.HeldShares * analysis.ClosePrice!.Value);
        var portfolioValuationComplete = alignedAnalyses
            .All(analysis => analysis.HeldShares <= 0 || analysis.ClosePrice is not null);
        var sectorMarketValues = Enumerable.Range(0, alignedAnalyses.Length)
            .Where(index => !string.IsNullOrWhiteSpace(watchlist[index].SectorCode))
            .GroupBy(index => watchlist[index].SectorCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(index =>
                    alignedAnalyses[index].HeldShares
                    * (alignedAnalyses[index].ClosePrice ?? 0m)),
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
        var recommendations = alignedAnalyses
            .Select(analysis => new StockRecommendationResult(
                analysis,
                0,
                0,
                0m,
                0m))
            .ToArray();

        var orderedIndexes = Enumerable.Range(0, alignedAnalyses.Length)
            .OrderBy(index => GetPricePriority(alignedAnalyses[index].PriceZoneCode))
            .ThenBy(index =>
                alignedAnalyses[index].DividendReliabilityCode == DividendReliabilityCodes.Passed
                    ? 0
                    : 1)
            .ThenByDescending(index => GetTargetGap(watchlist[index].Holding))
            .ThenBy(index => index)
            .ToArray();

        foreach (var index in orderedIndexes)
        {
            var analysis = alignedAnalyses[index];
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
            var recommendation = new StockRecommendationResult(
                analysis,
                trade.SuggestedBuyShares,
                trade.SuggestedSellShares,
                trade.SuggestedTradeAmount,
                trade.EstimatedTransactionFeeAmount);
            if (trade.SuggestedBuyShares == 0
                && IsBuyZone(priceZoneCode)
                && analysis.ModelStatusCode == ModelStatusCodes.Available
                && analysis.DividendReliabilityCode == DividendReliabilityCodes.Passed)
            {
                recommendation = recommendation with
                {
                    Analysis = analysis with
                    {
                        Explanation = !portfolioValuationComplete
                            ? $"{analysis.Explanation} 组合中存在缺少有效收盘价的持仓，本期不生成买入建议。"
                            : $"{analysis.Explanation} 本期组合预算或仓位额度不足，建议股数为 0。"
                    }
                };
            }

            if (trade.SuggestedBuyShares > 0)
            {
                remainingBudget = Math.Max(
                    remainingBudget
                        - trade.SuggestedTradeAmount
                        - trade.EstimatedTransactionFeeAmount,
                    0m);
            }

            totalSuggestedTradeAmount += trade.SuggestedTradeAmount;
            totalTransactionFeeAmount += trade.EstimatedTransactionFeeAmount;
            recommendations[index] = recommendation;
        }

        return new PortfolioRecommendationResult(
            budgetSummary.PortfolioId,
            startingAvailableBudget,
            remainingBudget,
            totalSuggestedTradeAmount,
            totalTransactionFeeAmount,
            recommendations,
            computedAt);
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
