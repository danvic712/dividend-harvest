namespace DividendHarvest.Domain.Portfolio;

public static class TradeDirectionCodes
{
    public static bool IsSupported(string? value)
        => value?.Trim().ToLowerInvariant() is "buy" or "sell";
}
