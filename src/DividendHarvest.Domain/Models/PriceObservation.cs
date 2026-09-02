namespace DividendHarvest.Domain.Models;

public sealed class PriceObservation
{
    private PriceObservation()
    {
    }

    public Guid Id { get; private set; }

    public Guid SecurityId { get; private set; }

    public DateOnly TradingDate { get; private set; }

    public decimal ClosePrice { get; private set; }

    public DateTimeOffset PriceObservedAt { get; private set; }

    public string DataSource { get; private set; } = string.Empty;

    public string SourceRecordId { get; private set; } = string.Empty;

    public string DataQualityCode { get; private set; } = string.Empty;

    public static PriceObservation Create(
        Guid securityId,
        DateOnly tradingDate,
        decimal closePrice,
        DateTimeOffset priceObservedAt,
        string dataSource,
        string sourceRecordId,
        string dataQualityCode)
    {
        if (securityId == Guid.Empty)
        {
            throw new ArgumentException("股票标识不能为空。", nameof(securityId));
        }

        if (tradingDate == DateOnly.MinValue)
        {
            throw new ArgumentException("交易日期不能为空。", nameof(tradingDate));
        }

        if (closePrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(closePrice),
                closePrice,
                "收盘价必须大于零。");
        }

        if (priceObservedAt == default)
        {
            throw new ArgumentException("行情观测时间不能为空。", nameof(priceObservedAt));
        }

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new ArgumentException("数据来源不能为空。", nameof(dataSource));
        }

        if (string.IsNullOrWhiteSpace(sourceRecordId))
        {
            throw new ArgumentException("来源记录标识不能为空。", nameof(sourceRecordId));
        }

        if (string.IsNullOrWhiteSpace(dataQualityCode))
        {
            throw new ArgumentException("数据质量代码不能为空。", nameof(dataQualityCode));
        }

        return new PriceObservation
        {
            Id = Guid.NewGuid(),
            SecurityId = securityId,
            TradingDate = tradingDate,
            ClosePrice = closePrice,
            PriceObservedAt = priceObservedAt.ToUniversalTime(),
            DataSource = dataSource.Trim(),
            SourceRecordId = sourceRecordId.Trim(),
            DataQualityCode = dataQualityCode.Trim()
        };
    }
}
