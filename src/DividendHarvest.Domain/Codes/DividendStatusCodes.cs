namespace DividendHarvest.Domain.Codes;

public static class DividendStatusCodes
{
    public const string Implemented = "implemented";

    public const string Proposed = "proposed";

    public const string Cancelled = "cancelled";

    public static bool IsSupported(string? code)
        => code is Implemented or Proposed or Cancelled;
}
