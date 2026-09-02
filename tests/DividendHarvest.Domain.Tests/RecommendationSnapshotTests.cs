using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class RecommendationSnapshotTests
{
    [Fact]
    public void Create_normalizes_status_codes_and_timestamps()
    {
        var computedAt = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.FromHours(8));

        var result = RecommendationSnapshot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            4m,
            0.4m,
            " TTM ",
            " AVAILABLE ",
            " PASSED ",
            " STRONG_BUY ",
            " STRONG_BUY ",
            0.1m,
            100,
            0,
            400m,
            5m,
            computedAt,
            Guid.NewGuid());

        Assert.Equal("available", result.ModelStatusCode);
        Assert.Equal("passed", result.DividendReliabilityCode);
        Assert.Equal("ttm", result.DividendModeCode);
        Assert.Equal("strong_buy", result.PriceZoneCode);
        Assert.Equal(computedAt.ToUniversalTime(), result.ComputedAt);
    }

    [Fact]
    public void Create_rejects_negative_trade_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecommendationSnapshot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            "unavailable",
            "unavailable",
            null,
            "no_action",
            null,
            0,
            0,
            -1m,
            0m,
            DateTimeOffset.UtcNow,
            null));
    }
}
