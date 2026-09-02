namespace DividendHarvest.Domain.Codes;

public static class DataQualityCodes
{
    public const string Valid = "valid";

    public const string Cautious = "cautious";

    public const string Stale = "stale";

    public const string Missing = "missing";

    public const string Conflicted = "conflicted";

    public static bool IsSupported(string? code)
        => code is Valid or Cautious or Stale or Missing or Conflicted;
}
