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
            true,
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
            null,
            false,
            "no_action",
            null,
            0,
            0,
            -1m,
            0m,
            DateTimeOffset.UtcNow,
            null));
    }

    [Fact]
    public void Create_rejects_re_evaluate_as_a_dividend_reliability_code()
    {
        Assert.Throws<ArgumentException>(() => RecommendationSnapshot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            4m,
            0.4m,
            "ttm",
            "re_evaluate",
            "re_evaluate",
            "strong_buy",
            "strong_buy",
            true,
            "re_evaluate",
            0.1m,
            0,
            0,
            0m,
            0m,
            DateTimeOffset.UtcNow,
            null));
    }

    [Fact]
    public void Create_accepts_model_re_evaluate_with_failed_reliability()
    {
        var result = RecommendationSnapshot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            4m,
            0.4m,
            "ttm",
            "re_evaluate",
            "failed",
            "strong_buy",
            "strong_buy",
            true,
            "re_evaluate",
            0.1m,
            0,
            0,
            0m,
            0m,
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal("re_evaluate", result.ModelStatusCode);
        Assert.Equal("failed", result.DividendReliabilityCode);
        Assert.Equal("re_evaluate", result.RecommendationCode);
    }

    [Fact]
    public void Create_accepts_model_re_evaluate_with_cautious_reliability()
    {
        var result = RecommendationSnapshot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            4m,
            0.4m,
            "ttm",
            "re_evaluate",
            "cautious",
            "strong_buy",
            "strong_buy",
            true,
            "re_evaluate",
            0.1m,
            0,
            0,
            0m,
            0m,
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal("re_evaluate", result.ModelStatusCode);
        Assert.Equal("cautious", result.DividendReliabilityCode);
    }

    [Fact]
    public void Create_rejects_a_recommendation_that_does_not_match_the_status()
    {
        Assert.Throws<ArgumentException>(() => RecommendationSnapshot.Create(
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
            null,
            false,
            "hold",
            null,
            0,
            0,
            0m,
            0m,
            DateTimeOffset.UtcNow,
            null));
    }
}
