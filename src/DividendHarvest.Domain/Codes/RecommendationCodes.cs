namespace DividendHarvest.Domain.Codes;

public static class RecommendationCodes
{
    public const string StrongBuy = "strong_buy";

    public const string Accumulate = "accumulate";

    public const string Hold = "hold";

    public const string PartialTrim = "partial_trim";

    public const string AggressiveTrim = "aggressive_trim";

    public const string ReEvaluate = "re_evaluate";

    public const string NoAction = "no_action";

    public static bool IsSupported(string? code)
        => code is StrongBuy
            or Accumulate
            or Hold
            or PartialTrim
            or AggressiveTrim
            or ReEvaluate
            or NoAction;
}
