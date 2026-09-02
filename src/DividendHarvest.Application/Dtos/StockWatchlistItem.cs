namespace DividendHarvest.Application.Dtos;

public sealed record StockWatchlistItem(
    string SecurityCode,
    string ExchangeCode,
    string SecurityName,
    string MarketCode,
    string CurrencyCode,
    StockHoldingSnapshot? Holding,
    string? SectorCode = null);
