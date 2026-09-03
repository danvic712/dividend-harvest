namespace DividendHarvest.Application.Dtos;

public sealed record StockFactSyncResult(
    string SecurityCode,
    string ExchangeCode,
    StockPriceObservationResult? PriceObservation,
    IReadOnlyList<StockDividendEventResult> DividendEvents,
    IReadOnlyList<StockFinancialSnapshotResult> FinancialSnapshots,
    IReadOnlyList<StockDataSyncFailure> Failures);
