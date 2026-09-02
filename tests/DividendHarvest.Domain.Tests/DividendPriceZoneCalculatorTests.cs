using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class DividendPriceZoneCalculatorTests
{
    [Fact]
    public void Calculate_classifies_a_price_at_the_strong_buy_boundary()
    {
        var parameters = ModelParameterSet.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "v1",
            0.08m,
            0.06m,
            0.04m,
            0.03m,
            0.5m,
            0.25m,
            0.25m,
            0.5m,
            0.2m,
            0.4m,
            0.1m,
            1000m,
            5000m,
            0.001m,
            5m,
            100,
            new DateOnly(2026, 9, 2));

        var result = DividendPriceZoneCalculator.Calculate(parameters, 0.32m, 4m);

        Assert.Equal(4m, result.StrongBuyPrice);
        Assert.Equal(0.08m, result.DividendYield);
        Assert.Equal("strong_buy", result.PriceZoneCode);
    }
}
