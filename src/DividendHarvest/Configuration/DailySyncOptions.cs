namespace DividendHarvest.Configuration;

public sealed class DailySyncOptions
{
    public const string SectionName = "DailySync";

    public bool Enabled { get; set; } = true;

    public string LocalTime { get; set; } = "18:00";

    public string TimeZoneId { get; set; } = "Asia/Shanghai";
}
