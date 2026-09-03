using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.DividendStrategy;

public sealed class PortfolioRecommendationAppService(
    IStockWatchlistAppService stockWatchlistAppService,
    IStockAnalysisAppService stockAnalysisAppService,
    IBudgetAppService budgetAppService,
    IPortfolioAllocationAppService portfolioAllocationAppService)
    : IPortfolioRecommendationAppService
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
        return await portfolioAllocationAppService.RunAsync(
            watchlist,
            analyses,
            budgetSummary,
            cancellationToken);
    }
}
