namespace DividendHarvest.Domain.Codes;

public static class DividendReliabilityCodes
{
    public const string Passed = "passed";

    public const string Cautious = "cautious";

    public const string Failed = "failed";

    public const string Unavailable = "unavailable";

    public static bool IsSupported(string? code)
        => code is Passed or Cautious or Failed or Unavailable;
}
