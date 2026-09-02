using DividendHarvest.Domain.Codes;

namespace DividendHarvest.Domain.Models;

public sealed class FinancialSnapshot
{
    private FinancialSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid SecurityId { get; private set; }

    public DateOnly DataAsOfDate { get; private set; }

    public DateTimeOffset CapturedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public decimal? EarningsPerShare { get; private set; }

    public decimal? DividendPayoutRatio { get; private set; }

    public decimal? ThreeYearAverageDividendPayoutRatio { get; private set; }

    public decimal? PriceToBookRatio { get; private set; }

    public decimal? ReturnOnEquity { get; private set; }

    public string DataSource { get; private set; } = string.Empty;

    public string SourceRecordId { get; private set; } = string.Empty;

    public string DataQualityCode { get; private set; } = string.Empty;

    public static FinancialSnapshot Create(
        Guid securityId,
        DateOnly dataAsOfDate,
        DateTimeOffset capturedAt,
        DateTimeOffset? publishedAt,
        decimal? earningsPerShare,
        decimal? dividendPayoutRatio,
        decimal? threeYearAverageDividendPayoutRatio,
        decimal? priceToBookRatio,
        decimal? returnOnEquity,
        string dataSource,
        string sourceRecordId,
        string dataQualityCode)
    {
        if (securityId == Guid.Empty)
        {
            throw new ArgumentException("股票标识不能为空。", nameof(securityId));
        }

        if (dataAsOfDate == DateOnly.MinValue)
        {
            throw new ArgumentException("数据截至日期不能为空。", nameof(dataAsOfDate));
        }

        if (capturedAt == default)
        {
            throw new ArgumentException("抓取时间不能为空。", nameof(capturedAt));
        }

        if (publishedAt is { } published && published == default)
        {
            throw new ArgumentException("财务数据公开时间不能为空。", nameof(publishedAt));
        }

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new ArgumentException("数据来源不能为空。", nameof(dataSource));
        }

        if (string.IsNullOrWhiteSpace(sourceRecordId))
        {
            throw new ArgumentException("来源记录标识不能为空。", nameof(sourceRecordId));
        }

        var normalizedQualityCode = dataQualityCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!DataQualityCodes.IsSupported(normalizedQualityCode))
        {
            throw new ArgumentException(
                "数据质量代码不受支持。",
                nameof(dataQualityCode));
        }

        return new FinancialSnapshot
        {
            Id = Guid.NewGuid(),
            SecurityId = securityId,
            DataAsOfDate = dataAsOfDate,
            CapturedAt = capturedAt.ToUniversalTime(),
            PublishedAt = publishedAt?.ToUniversalTime(),
            EarningsPerShare = earningsPerShare,
            DividendPayoutRatio = dividendPayoutRatio,
            ThreeYearAverageDividendPayoutRatio = threeYearAverageDividendPayoutRatio,
            PriceToBookRatio = priceToBookRatio,
            ReturnOnEquity = returnOnEquity,
            DataSource = dataSource.Trim(),
            SourceRecordId = sourceRecordId.Trim(),
            DataQualityCode = normalizedQualityCode
        };
    }
}
