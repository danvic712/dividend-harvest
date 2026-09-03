using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IPortfolioAllocationAppService
{
    Task<PortfolioRecommendationResult> RunAsync(
        IReadOnlyList<StockWatchlistItem> watchlist,
        IReadOnlyList<StockAnalysisResult> analyses,
        BudgetSummary budgetSummary,
        CancellationToken cancellationToken);
}
