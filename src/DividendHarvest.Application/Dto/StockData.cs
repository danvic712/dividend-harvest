namespace DividendHarvest.Application.Dto;

public sealed record StockData(
    string SecurityCode,
    string ExchangeCode,
    string SecurityName,
    string MarketCode,
    string CurrencyCode);
