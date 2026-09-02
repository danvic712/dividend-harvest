namespace DividendHarvest.Domain.Codes;

public static class PriceZoneCodes
{
    public const string StrongBuy = "strong_buy";

    public const string Accumulate = "accumulate";

    public const string Hold = "hold";

    public const string PartialTrim = "partial_trim";

    public const string AggressiveTrim = "aggressive_trim";

    public static bool IsSupported(string? code)
        => code is StrongBuy or Accumulate or Hold or PartialTrim or AggressiveTrim;
}
