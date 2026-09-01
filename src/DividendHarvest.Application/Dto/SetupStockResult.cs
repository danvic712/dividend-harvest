namespace DividendHarvest.Application.Dto;

public sealed record SetupStockResult(
    string SecurityCode,
    string ExchangeCode,
    string SecurityName);
