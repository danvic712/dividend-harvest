namespace DividendHarvest.Domain.Portfolio;

public static class CashLedgerCodes
{
    public static bool IsSupportedEntryType(string? value)
        => value?.Trim().ToLowerInvariant() is
            "budget_deposit" or
            "dividend_received" or
            "buy" or
            "sell" or
            "fee" or
            "cash_adjustment";

    public static bool IsSupportedDirection(string? value)
        => value?.Trim().ToLowerInvariant() is "inflow" or "outflow";

    public static bool IsCompatible(string? entryTypeCode, string? cashDirectionCode)
    {
        var entryType = entryTypeCode?.Trim().ToLowerInvariant();
        var direction = cashDirectionCode?.Trim().ToLowerInvariant();

        return (entryType, direction) switch
        {
            ("budget_deposit", "inflow") => true,
            ("dividend_received", "inflow") => true,
            ("sell", "inflow") => true,
            ("buy", "outflow") => true,
            ("fee", "outflow") => true,
            ("cash_adjustment", "inflow" or "outflow") => true,
            _ => false
        };
    }
}
