namespace DividendHarvest.Domain.Codes;

public static class DividendModeCodes
{
    public const string Ttm = "ttm";

    public const string Forward = "forward";

    public const string Custom = "custom";

    public static bool IsSupported(string? code)
        => code is Ttm or Forward or Custom;
}
