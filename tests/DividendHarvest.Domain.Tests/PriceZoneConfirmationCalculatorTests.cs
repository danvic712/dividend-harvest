using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class PriceZoneConfirmationCalculatorTests
{
    [Fact]
    public void Calculate_confirms_a_zone_after_two_valid_trading_days()
    {
        var securityId = Guid.NewGuid();
        var parameters = CreateParameters(Guid.NewGuid(), securityId);
        var observations = new[]
        {
            CreateObservation(securityId, new DateOnly(2026, 9, 2), 4m, "price-2"),
            CreateObservation(securityId, new DateOnly(2026, 9, 1), 4.1m, "price-1")
        };

        var result = PriceZoneConfirmationCalculator.Calculate(parameters, 0.40m, observations);

        Assert.Equal("strong_buy", result.ObservedPriceZoneCode);
        Assert.Equal("strong_buy", result.ConfirmedPriceZoneCode);
        Assert.True(result.IsConfirmed);
    }

    [Fact]
    public void Calculate_does_not_confirm_when_the_latest_zone_changed()
    {
        var securityId = Guid.NewGuid();
        var parameters = CreateParameters(Guid.NewGuid(), securityId);
        var observations = new[]
        {
            CreateObservation(securityId, new DateOnly(2026, 9, 2), 4m, "price-2"),
            CreateObservation(securityId, new DateOnly(2026, 9, 1), 5.5m, "price-1")
        };

        var result = PriceZoneConfirmationCalculator.Calculate(parameters, 0.40m, observations);

        Assert.Equal("strong_buy", result.ObservedPriceZoneCode);
        Assert.Null(result.ConfirmedPriceZoneCode);
        Assert.False(result.IsConfirmed);
    }

    [Fact]
    public void Calculate_groups_duplicate_observations_before_taking_two_trading_days()
    {
        var securityId = Guid.NewGuid();
        var parameters = CreateParameters(Guid.NewGuid(), securityId);
        var observations = new[]
        {
            CreateObservation(securityId, new DateOnly(2026, 9, 2), 4m, "price-2a"),
            CreateObservation(securityId, new DateOnly(2026, 9, 2), 4m, "price-2b"),
            CreateObservation(securityId, new DateOnly(2026, 9, 1), 4m, "price-1")
        };

        var result = PriceZoneConfirmationCalculator.Calculate(parameters, 0.40m, observations);

        Assert.True(result.IsConfirmed);
        Assert.Equal("strong_buy", result.ConfirmedPriceZoneCode);
    }

    private static PriceObservation CreateObservation(
        Guid securityId,
        DateOnly tradingDate,
        decimal closePrice,
        string sourceRecordId)
        => PriceObservation.Create(
            securityId,
            tradingDate,
            closePrice,
            tradingDate.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc),
            "FTShare",
            sourceRecordId,
            "valid");

    private static ModelParameterSet CreateParameters(Guid portfolioId, Guid securityId)
        => ModelParameterSet.Create(
            portfolioId,
            securityId,
            "v1",
            0.08m,
            0.06m,
            0.04m,
            0.03m,
            0.5m,
            0.25m,
            0.25m,
            0.5m,
            0.5m,
            0.8m,
            0m,
            3000m,
            5000m,
            0.001m,
            5m,
            100,
            new DateOnly(2026, 1, 1));
}
