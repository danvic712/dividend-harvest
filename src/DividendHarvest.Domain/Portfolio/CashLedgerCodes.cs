namespace DividendHarvest.Domain.Portfolio;

public static class CashLedgerCodes
{
    public const string BudgetDeposit = "budget_deposit";

    public const string DividendReceived = "dividend_received";

    public const string Buy = "buy";

    public const string Sell = "sell";

    public const string Fee = "fee";

    public const string CashAdjustment = "cash_adjustment";

    public const string Inflow = "inflow";

    public const string Outflow = "outflow";

    public static bool IsSupportedEntryType(string? value)
        => value?.Trim().ToLowerInvariant() is
            BudgetDeposit or
            DividendReceived or
            Buy or
            Sell or
            Fee or
            CashAdjustment;

    public static bool IsSupportedDirection(string? value)
        => value?.Trim().ToLowerInvariant() is Inflow or Outflow;

    public static bool IsCompatible(string? entryTypeCode, string? cashDirectionCode)
    {
        var entryType = entryTypeCode?.Trim().ToLowerInvariant();
        var direction = cashDirectionCode?.Trim().ToLowerInvariant();

        return (entryType, direction) switch
        {
            (BudgetDeposit, Inflow) => true,
            (DividendReceived, Inflow) => true,
            (Sell, Inflow) => true,
            (Buy, Outflow) => true,
            (Fee, Outflow) => true,
            (CashAdjustment, Inflow or Outflow) => true,
            _ => false
        };
    }
}
