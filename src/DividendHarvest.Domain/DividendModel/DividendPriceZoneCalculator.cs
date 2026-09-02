using DividendHarvest.Domain.Models;

namespace DividendHarvest.Domain.DividendModel;

public static class DividendPriceZoneCalculator
{
    public static PriceZoneResult Calculate(
        ModelParameterSet parameters,
        decimal modelDividendPerShare,
        decimal closePrice)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (modelDividendPerShare <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modelDividendPerShare),
                modelDividendPerShare,
                "模型股息必须大于零。");
        }

        if (closePrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(closePrice),
                closePrice,
                "当前价格必须大于零。");
        }

        var strongBuyPrice =
            modelDividendPerShare / parameters.StrongBuyYieldThreshold;
        var accumulatePrice =
            modelDividendPerShare / parameters.AccumulationYieldThreshold;
        var partialTrimPrice =
            modelDividendPerShare / parameters.PartialTrimYieldThreshold;
        var aggressiveTrimPrice =
            modelDividendPerShare / parameters.AggressiveTrimYieldThreshold;
        var priceZoneCode = closePrice <= strongBuyPrice
            ? "strong_buy"
            : closePrice <= accumulatePrice
                ? "accumulate"
                : closePrice < partialTrimPrice
                    ? "hold"
                    : closePrice < aggressiveTrimPrice
                        ? "partial_trim"
                        : "aggressive_trim";

        return new PriceZoneResult(
            strongBuyPrice,
            accumulatePrice,
            partialTrimPrice,
            aggressiveTrimPrice,
            modelDividendPerShare / closePrice,
            priceZoneCode);
    }
}
