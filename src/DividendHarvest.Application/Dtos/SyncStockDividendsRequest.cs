namespace DividendHarvest.Application.Dtos;

public sealed record SyncStockDividendsRequest(
    string SecurityCode,
    string ExchangeCode);
