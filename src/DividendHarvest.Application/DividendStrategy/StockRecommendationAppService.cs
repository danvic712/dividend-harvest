using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.DividendStrategy;

public sealed class StockRecommendationAppService(
    IStockAnalysisAppService stockAnalysisAppService,
    IStockWatchlistAppService stockWatchlistAppService,
    IBudgetAppService budgetAppService,
    IPortfolioAllocationAppService portfolioAllocationAppService)
    : IStockRecommendationAppService
{
    public async Task<StockRecommendationResult> GetAsync(
        GetStockAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var analysis = await stockAnalysisAppService.GetAsync(
            request,
            cancellationToken);
        var watchlist = await stockWatchlistAppService.GetAsync(cancellationToken);
        var requestedStock = watchlist.FirstOrDefault(item =>
            item.SecurityCode == analysis.SecurityCode
            && item.ExchangeCode == analysis.ExchangeCode
            && item.SecurityId == analysis.SecurityId);
        if (requestedStock is null)
        {
            return new StockRecommendationResult(analysis, 0, 0, 0m, 0m);
        }

        var analyses = new List<StockAnalysisResult>(watchlist.Count);
        foreach (var stock in watchlist)
        {
            analyses.Add(stock.SecurityId == analysis.SecurityId
                ? analysis
                : await stockAnalysisAppService.GetAsync(
                    new GetStockAnalysisRequest(
                        stock.SecurityCode,
                        stock.ExchangeCode),
                    cancellationToken));
        }

        var budgetSummary = await budgetAppService.GetSummaryAsync(cancellationToken);
        var recommendation = await portfolioAllocationAppService.RunAsync(
            watchlist,
            analyses,
            budgetSummary,
            cancellationToken);

        return recommendation.Stocks
            .Single(stock => stock.Analysis.SecurityId == analysis.SecurityId);
    }
}
