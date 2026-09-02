namespace DividendHarvest.Application.Dtos;

public sealed record PortfolioRecommendationResult(
    Guid PortfolioId,
    decimal StartingAvailableBudgetAmount,
    decimal RemainingAvailableBudgetAmount,
    decimal TotalSuggestedTradeAmount,
    decimal EstimatedTransactionFeeAmount,
    IReadOnlyList<StockAnalysisResult> Stocks,
    DateTimeOffset ComputedAt);
