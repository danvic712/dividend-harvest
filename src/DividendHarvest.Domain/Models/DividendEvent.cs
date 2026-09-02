namespace DividendHarvest.Domain.Models;

public sealed class DividendEvent
{
    private static readonly string[] SupportedDividendTypes =
        ["regular_cash", "special_cash"];

    private static readonly string[] SupportedDividendStatuses =
        ["implemented", "proposed", "cancelled"];

    private static readonly string[] SupportedDataQualityCodes =
        ["valid", "cautious", "stale", "missing", "conflicted"];

    private DividendEvent()
    {
    }

    public Guid Id { get; private set; }

    public Guid SecurityId { get; private set; }

    public decimal DividendPerShare { get; private set; }

    public string DividendTypeCode { get; private set; } = string.Empty;

    public string DividendStatusCode { get; private set; } = string.Empty;

    public DateOnly? AnnouncementDate { get; private set; }

    public DateOnly? ExDividendDate { get; private set; }

    public DateOnly? PaymentDate { get; private set; }

    public bool IsSpecialDividend { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CapturedAt { get; private set; }

    public string DataSource { get; private set; } = string.Empty;

    public string SourceRecordId { get; private set; } = string.Empty;

    public string DataQualityCode { get; private set; } = string.Empty;

    public static DividendEvent Create(
        Guid securityId,
        decimal dividendPerShare,
        string dividendTypeCode,
        string dividendStatusCode,
        DateOnly? announcementDate,
        DateOnly? exDividendDate,
        DateOnly? paymentDate,
        bool isSpecialDividend,
        DateTimeOffset? publishedAt,
        DateTimeOffset capturedAt,
        string dataSource,
        string sourceRecordId,
        string dataQualityCode)
    {
        if (securityId == Guid.Empty)
        {
            throw new ArgumentException("股票标识不能为空。", nameof(securityId));
        }

        if (dividendPerShare <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dividendPerShare),
                dividendPerShare,
                "每股股息必须大于零。");
        }

        var normalizedDividendType = dividendTypeCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!SupportedDividendTypes.Contains(normalizedDividendType, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "股息类型必须是 regular_cash 或 special_cash。",
                nameof(dividendTypeCode));
        }

        var normalizedDividendStatus = dividendStatusCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!SupportedDividendStatuses.Contains(normalizedDividendStatus, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "股息状态必须是 implemented、proposed 或 cancelled。",
                nameof(dividendStatusCode));
        }

        ValidateDate(announcementDate, nameof(announcementDate));
        ValidateDate(exDividendDate, nameof(exDividendDate));
        ValidateDate(paymentDate, nameof(paymentDate));

        if (publishedAt is { } published && published == default)
        {
            throw new ArgumentException("股息公开时间不能为空。", nameof(publishedAt));
        }

        if (capturedAt == default)
        {
            throw new ArgumentException("抓取时间不能为空。", nameof(capturedAt));
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
        if (!SupportedDataQualityCodes.Contains(normalizedQualityCode, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "数据质量代码不受支持。",
                nameof(dataQualityCode));
        }

        return new DividendEvent
        {
            Id = Guid.NewGuid(),
            SecurityId = securityId,
            DividendPerShare = dividendPerShare,
            DividendTypeCode = normalizedDividendType,
            DividendStatusCode = normalizedDividendStatus,
            AnnouncementDate = announcementDate,
            ExDividendDate = exDividendDate,
            PaymentDate = paymentDate,
            IsSpecialDividend = isSpecialDividend,
            PublishedAt = publishedAt?.ToUniversalTime(),
            CapturedAt = capturedAt.ToUniversalTime(),
            DataSource = dataSource.Trim(),
            SourceRecordId = sourceRecordId.Trim(),
            DataQualityCode = normalizedQualityCode
        };
    }

    private static void ValidateDate(DateOnly? date, string parameterName)
    {
        if (date == DateOnly.MinValue)
        {
            throw new ArgumentException("日期不能为空。", parameterName);
        }
    }
}
