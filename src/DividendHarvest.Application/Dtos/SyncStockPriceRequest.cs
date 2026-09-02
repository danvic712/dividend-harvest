namespace DividendHarvest.Application.Dtos;

public sealed record SyncStockPriceRequest(
    string SecurityCode,
    string ExchangeCode);
