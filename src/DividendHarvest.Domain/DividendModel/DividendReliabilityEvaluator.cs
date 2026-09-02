using DividendHarvest.Domain.Models;

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

        var recentCancellation = dividendEvents.Any(dividendEvent =>
            dividendEvent.DividendStatusCode == "cancelled"
            && dividendEvent.AnnouncementDate is { } announcementDate
            && announcementDate > dataAsOfDate.AddYears(-1)
            && announcementDate <= dataAsOfDate);
        if (recentCancellation)
        {
            return "re_evaluate";
        }

        var latestFinancialSnapshot = financialSnapshots
            .Where(snapshot => snapshot.DataAsOfDate <= dataAsOfDate)
            .OrderByDescending(snapshot => snapshot.DataAsOfDate)
            .FirstOrDefault();
        if (latestFinancialSnapshot?.DividendPayoutRatio is { } payoutRatio
            && (payoutRatio <= 0 || payoutRatio >= 1))
        {
            return "failed";
        }

        if (latestFinancialSnapshot?.ThreeYearAverageDividendPayoutRatio is { } averagePayoutRatio
            && (averagePayoutRatio <= 0 || averagePayoutRatio >= 1))
        {
            return "failed";
        }

        var completedYears = Enumerable.Range(1, 5)
            .Select(offset => dataAsOfDate.Year - offset)
            .ToArray();
        var annualDividends = dividendEvents
            .Where(IsUsableRegularDividend)
            .Where(dividendEvent =>
                dividendEvent.ExDividendDate is { } exDividendDate
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
            return "failed";
        }

        if (completedYears.Any(year => !annualDividends.ContainsKey(year)))
        {
            return "cautious";
        }

        var oldestYear = completedYears[^1];
        var latestYear = completedYears[0];
        if (annualDividends[latestYear] < annualDividends[oldestYear])
        {
            return "failed";
        }

        if (completedYears
            .Take(3)
            .Any(year => annualDividends[year] <= 0))
        {
            return "failed";
        }

        if (latestFinancialSnapshot is null
            || latestFinancialSnapshot.DataQualityCode != "valid"
            || latestFinancialSnapshot.DividendPayoutRatio is null
            || latestFinancialSnapshot.ThreeYearAverageDividendPayoutRatio is null)
        {
            return "cautious";
        }

        return "passed";
    }

    private static bool IsUsableRegularDividend(DividendEvent dividendEvent)
        => dividendEvent.DividendStatusCode == "implemented"
            && dividendEvent.DividendTypeCode == "regular_cash"
            && !dividendEvent.IsSpecialDividend
            && dividendEvent.DataQualityCode == "valid";
}
