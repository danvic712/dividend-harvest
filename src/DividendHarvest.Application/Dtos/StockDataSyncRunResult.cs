namespace DividendHarvest.Application.Dtos;

public sealed record StockDataSyncRunResult(
    int AttemptedStockCount,
    int FullyCompletedStockCount,
    int PartiallyFailedStockCount,
    IReadOnlyList<StockDataSyncFailure> Failures,
    DateTimeOffset CompletedAt);
