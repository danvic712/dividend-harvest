namespace DividendHarvest.Domain.Portfolio;

public static class TradeDirectionCodes
{
    public const string Buy = "buy";

    public const string Sell = "sell";

    public static bool IsSupported(string? value)
        => value?.Trim().ToLowerInvariant() is Buy or Sell;
}
