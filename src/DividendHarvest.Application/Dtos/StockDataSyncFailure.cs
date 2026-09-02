namespace DividendHarvest.Application.Dtos;

public sealed record StockDataSyncFailure(
    string SecurityCode,
    string ExchangeCode,
    string DataKind,
    string ErrorCode,
    string FailureMessage);
