using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class ModelParameterSetTests
{
    [Fact]
    public void Create_returns_a_trimmed_version_for_valid_parameters()
    {
        var result = ModelParameterSet.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " v1 ",
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

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("v1", result.ModelVersion);
        Assert.Equal(0.08m, result.StrongBuyYieldThreshold);
        Assert.Equal(100, result.TradingLotSize);
    }

    [Fact]
    public void Create_rejects_yield_thresholds_that_are_not_strictly_descending()
    {
        var exception = Assert.Throws<ArgumentException>(() => ModelParameterSet.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "v1",
            0.08m,
            0.06m,
            0.07m,
            0.04m,
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
            new DateOnly(2026, 9, 2)));

        Assert.Equal("partialTrimYieldThreshold", exception.ParamName);
    }
}
