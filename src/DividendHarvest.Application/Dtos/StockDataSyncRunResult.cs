namespace DividendHarvest.Application.Dtos;

public sealed record StockDataSyncRunResult(
    int AttemptedStockCount,
    int CompletedStockCount,
    int FailedStockCount,
    IReadOnlyList<StockDataSyncFailure> Failures,
    DateTimeOffset CompletedAt);
