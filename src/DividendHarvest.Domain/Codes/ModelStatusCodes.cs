namespace DividendHarvest.Domain.Codes;

public static class ModelStatusCodes
{
    public const string Available = "available";

    public const string Cautious = "cautious";

    public const string Failed = "failed";

    public const string Unavailable = "unavailable";

    public const string ReEvaluate = "re_evaluate";

    public static bool IsSupported(string? code)
        => code is Available or Cautious or Failed or Unavailable or ReEvaluate;
}
