using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Codes;

namespace DividendHarvest.Domain.DividendModel;

public static class DividendReliabilityEvaluator
{
    public static string Evaluate(
        IEnumerable<DividendEvent> dividendEvents,
        IEnumerable<FinancialSnapshot> financialSnapshots,
        DateOnly dataAsOfDate)
    {
        ArgumentNullException.ThrowIfNull(dividendEvents);
        ArgumentNullException.ThrowIfNull(financialSnapshots);

        if (dataAsOfDate == DateOnly.MinValue)
        {
            throw new ArgumentException(
                "可靠性数据截至日期不能为空。",
                nameof(dataAsOfDate));
        }

        if (HasRecentCancellation(dividendEvents, dataAsOfDate))
        {
            return DividendReliabilityCodes.Failed;
        }

        var latestFinancialSnapshot = financialSnapshots
            .Where(snapshot =>
                snapshot.DataAsOfDate <= dataAsOfDate
                && HistoricalDataAvailability.WasPublicBy(
                    snapshot.PublishedAt,
                    dataAsOfDate)
                && snapshot.DataQualityCode == DataQualityCodes.Valid)
            .OrderByDescending(snapshot => snapshot.DataAsOfDate)
            .FirstOrDefault();
        if (latestFinancialSnapshot?.DividendPayoutRatio is { } payoutRatio
            && (payoutRatio <= 0 || payoutRatio >= 1))
        {
            return DividendReliabilityCodes.Failed;
        }

        if (latestFinancialSnapshot?.ThreeYearAverageDividendPayoutRatio is { } averagePayoutRatio
            && (averagePayoutRatio <= 0 || averagePayoutRatio >= 1))
        {
            return DividendReliabilityCodes.Failed;
        }

        var completedYears = Enumerable.Range(1, 5)
            .Select(offset => dataAsOfDate.Year - offset)
            .ToArray();
        var annualDividends = dividendEvents
            .Where(IsUsableRegularDividend)
            .Where(dividendEvent =>
                HistoricalDataAvailability.WasPublicBy(
                    dividendEvent.PublishedAt,
                    dataAsOfDate)
                && dividendEvent.ExDividendDate is { } exDividendDate
                && exDividendDate <= dataAsOfDate
                && completedYears.Contains(exDividendDate.Year))
            .GroupBy(dividendEvent => dividendEvent.ExDividendDate!.Value.Year)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(dividendEvent => dividendEvent.DividendPerShare));

        if (completedYears
            .Take(3)
            .Any(year => !annualDividends.ContainsKey(year)))
        {
            return DividendReliabilityCodes.Failed;
        }

        if (completedYears.Any(year => !annualDividends.ContainsKey(year)))
        {
            return DividendReliabilityCodes.Cautious;
        }

        var oldestYear = completedYears[^1];
        var latestYear = completedYears[0];
        if (annualDividends[latestYear] < annualDividends[oldestYear])
        {
            return DividendReliabilityCodes.Failed;
        }

        if (completedYears
            .Take(3)
            .Any(year => annualDividends[year] <= 0))
        {
            return DividendReliabilityCodes.Failed;
        }

        if (latestFinancialSnapshot is null
            || latestFinancialSnapshot.DividendPayoutRatio is null
            || latestFinancialSnapshot.ThreeYearAverageDividendPayoutRatio is null)
        {
            return DividendReliabilityCodes.Cautious;
        }

        return DividendReliabilityCodes.Passed;
    }

    public static bool HasRecentCancellation(
        IEnumerable<DividendEvent> dividendEvents,
        DateOnly dataAsOfDate)
    {
        ArgumentNullException.ThrowIfNull(dividendEvents);

        if (dataAsOfDate == DateOnly.MinValue)
        {
            throw new ArgumentException(
                "可靠性数据截至日期不能为空。",
                nameof(dataAsOfDate));
        }

        return dividendEvents.Any(dividendEvent =>
            dividendEvent.DividendStatusCode == DividendStatusCodes.Cancelled
            && dividendEvent.DataQualityCode == DataQualityCodes.Valid
            && HistoricalDataAvailability.WasPublicBy(
                dividendEvent.PublishedAt,
                dataAsOfDate)
            && dividendEvent.AnnouncementDate is { } announcementDate
            && announcementDate > dataAsOfDate.AddYears(-1)
            && announcementDate <= dataAsOfDate);
    }

    private static bool IsUsableRegularDividend(DividendEvent dividendEvent)
        => dividendEvent.DividendStatusCode == DividendStatusCodes.Implemented
            && dividendEvent.DividendTypeCode == DividendTypeCodes.RegularCash
            && !dividendEvent.IsSpecialDividend
            && dividendEvent.DataQualityCode == DataQualityCodes.Valid;
}
