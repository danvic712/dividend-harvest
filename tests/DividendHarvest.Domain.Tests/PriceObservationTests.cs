using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class PriceObservationTests
{
    [Fact]
    public void Create_normalizes_and_validates_data_quality_code()
    {
        var result = PriceObservation.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            4m,
            new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
            "FTShare",
            "price-1",
            " VALID ");

        Assert.Equal("valid", result.DataQualityCode);
    }

    [Fact]
    public void Create_rejects_unknown_data_quality_code()
    {
        Assert.Throws<ArgumentException>(() => PriceObservation.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            4m,
            new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
            "FTShare",
            "price-1",
            "unverified"));
    }

    [Fact]
    public void Create_rejects_a_non_positive_close_price()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            PriceObservation.Create(
                Guid.NewGuid(),
                new DateOnly(2026, 9, 1),
                0m,
                new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
                "FTShare",
                "000001:2026-09-01",
                "valid"));

        Assert.Equal("closePrice", exception.ParamName);
    }
}
