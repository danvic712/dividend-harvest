namespace DividendHarvest.Application.Dtos;

public sealed record GetStockAnalysisRequest(
    string SecurityCode,
    string ExchangeCode);
