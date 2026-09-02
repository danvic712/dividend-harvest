namespace DividendHarvest.Application.Dtos;

public sealed record StockMarketData(
    string SecurityCode,
    string ExchangeCode,
    decimal ClosePrice,
    DateOnly TradingDate,
    DateTimeOffset PriceObservedAt,
    string DataSource,
    string SourceRecordId,
    string DataQualityCode);
