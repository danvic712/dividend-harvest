namespace DividendHarvest.Application.Dtos;

public sealed record CreateRecommendationSnapshotResult(
    Guid ModelRunId,
    Guid PortfolioId,
    int SnapshotCount,
    DateTimeOffset ComputedAt,
    IReadOnlyList<StockAnalysisResult> Stocks);
