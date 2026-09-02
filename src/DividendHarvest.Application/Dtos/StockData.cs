namespace DividendHarvest.Application.Dtos;

public sealed record StockData(
    string SecurityCode,
    string ExchangeCode,
    string SecurityName,
    string MarketCode,
    string CurrencyCode);
