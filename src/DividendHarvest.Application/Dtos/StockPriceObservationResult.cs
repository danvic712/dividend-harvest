namespace DividendHarvest.Application.Dtos;

public sealed record StockPriceObservationResult(
    Guid PriceObservationId,
    string SecurityCode,
    string ExchangeCode,
    DateOnly TradingDate,
    decimal ClosePrice,
    DateTimeOffset PriceObservedAt,
    string DataSource,
    string SourceRecordId,
    string DataQualityCode);
