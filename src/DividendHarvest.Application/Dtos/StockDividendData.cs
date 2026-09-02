namespace DividendHarvest.Application.Dtos;

public sealed record StockDividendData(
    string SecurityCode,
    string ExchangeCode,
    decimal DividendPerShare,
    string DividendTypeCode,
    string DividendStatusCode,
    DateOnly? AnnouncementDate,
    DateOnly? ExDividendDate,
    DateOnly? PaymentDate,
    bool IsSpecialDividend,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CapturedAt,
    string DataSource,
    string SourceRecordId,
    string DataQualityCode);
