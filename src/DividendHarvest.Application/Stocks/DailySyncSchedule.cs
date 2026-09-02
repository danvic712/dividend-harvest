namespace DividendHarvest.Application.Stocks;

public static class DailySyncSchedule
{
    public static DateTimeOffset GetNextRunUtc(
        DateTimeOffset utcNow,
        TimeOnly localRunTime,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var nextDate = DateOnly.FromDateTime(localNow.DateTime);
        if (localNow.TimeOfDay >= localRunTime.ToTimeSpan())
        {
            nextDate = nextDate.AddDays(1);
        }

        while (nextDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            nextDate = nextDate.AddDays(1);
        }

        var nextLocal = nextDate.ToDateTime(localRunTime, DateTimeKind.Unspecified);
        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, timeZone);
        return new DateTimeOffset(nextUtc, TimeSpan.Zero);
    }
}
