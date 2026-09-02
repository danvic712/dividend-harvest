namespace DividendHarvest.Application.Dtos;

public sealed record StockFinancialData(
    string SecurityCode,
    string ExchangeCode,
    DateOnly DataAsOfDate,
    DateTimeOffset CapturedAt,
    DateTimeOffset? PublishedAt,
    decimal? EarningsPerShare,
    decimal? DividendPayoutRatio,
    decimal? ThreeYearAverageDividendPayoutRatio,
    decimal? PriceToBookRatio,
    decimal? ReturnOnEquity,
    string DataSource,
    string SourceRecordId,
    string DataQualityCode);
