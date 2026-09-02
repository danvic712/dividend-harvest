namespace DividendHarvest.Domain.Codes;

public static class DividendTypeCodes
{
    public const string RegularCash = "regular_cash";

    public const string SpecialCash = "special_cash";

    public static bool IsSupported(string? code)
        => code is RegularCash or SpecialCash;
}
