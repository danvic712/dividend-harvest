using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Codes;

namespace DividendHarvest.Domain.DividendModel;

public static class TtmDividendCalculator
{
    public static decimal? Calculate(
        IEnumerable<DividendEvent> dividendEvents,
        DateOnly dataAsOfDate)
    {
        ArgumentNullException.ThrowIfNull(dividendEvents);

        if (dataAsOfDate == DateOnly.MinValue)
        {
            throw new ArgumentException(
                "股息数据截至日期不能为空。",
                nameof(dataAsOfDate));
        }

        var windowStart = dataAsOfDate.AddYears(-1).AddDays(1);
        var total = dividendEvents
            .Where(dividendEvent =>
                dividendEvent.DividendStatusCode == DividendStatusCodes.Implemented
                && dividendEvent.DividendTypeCode == DividendTypeCodes.RegularCash
                && !dividendEvent.IsSpecialDividend
                && dividendEvent.DataQualityCode == DataQualityCodes.Valid
                && HistoricalDataAvailability.WasPublicBy(
                    dividendEvent.PublishedAt,
                    dataAsOfDate)
                && dividendEvent.ExDividendDate is { } exDividendDate
                && exDividendDate >= windowStart
                && exDividendDate <= dataAsOfDate)
            .Sum(dividendEvent => dividendEvent.DividendPerShare);

        return total > 0 ? total : null;
    }
}
