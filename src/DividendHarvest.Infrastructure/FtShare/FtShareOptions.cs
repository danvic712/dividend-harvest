namespace DividendHarvest.Infrastructure.FtShare;

public sealed class FtShareOptions
{
    public const string SectionName = "FtShare";

    public string McpEndpoint { get; set; } = string.Empty;

    public string StockProfileToolName { get; set; } = "get_stock_profile";

    public string StockMarketDataToolName { get; set; } = "get_stock_market_data";

    public string StockDividendEventsToolName { get; set; } = "get_stock_dividend_events";

    public string SecurityCodeArgumentName { get; set; } = "security_code";

    public string ExchangeCodeArgumentName { get; set; } = "exchange_code";

    public int RequestTimeoutSeconds { get; set; } = 30;

    public TimeSpan RequestTimeout => TimeSpan.FromSeconds(
        Math.Clamp(RequestTimeoutSeconds, 1, 300));
}
