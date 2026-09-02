namespace DividendHarvest.Application.Dtos;

public sealed record GetStockModelParametersRequest(
    string SecurityCode,
    string ExchangeCode);
