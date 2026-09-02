namespace DividendHarvest.Application.Dtos;

public sealed record SyncStockFinancialsRequest(
    string SecurityCode,
    string ExchangeCode);
