namespace DividendHarvest.Domain.DividendModel;

public static class HistoricalDataAvailability
{
    public static bool WasPublicBy(
        DateTimeOffset? publishedAt,
        DateOnly dataAsOfDate)
    {
        if (dataAsOfDate == DateOnly.MinValue)
        {
            throw new ArgumentException(
                "历史数据截至日期不能为空。",
                nameof(dataAsOfDate));
        }

        return publishedAt is null
            || DateOnly.FromDateTime(publishedAt.Value.UtcDateTime) <= dataAsOfDate;
    }
}
