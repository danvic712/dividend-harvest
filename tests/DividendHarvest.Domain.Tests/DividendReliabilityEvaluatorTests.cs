using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class DividendReliabilityEvaluatorTests
{
    [Fact]
    public void Evaluate_returns_cautious_when_five_year_dividend_history_is_incomplete()
    {
        var securityId = Guid.NewGuid();
        var events = new[]
        {
            CreateDividendEvent(securityId, 0.20m, new DateOnly(2026, 6, 1), "current"),
            CreateDividendEvent(securityId, 0.20m, new DateOnly(2025, 6, 1), "previous"),
            CreateDividendEvent(securityId, 0.20m, new DateOnly(2024, 6, 1), "year-2024"),
            CreateDividendEvent(securityId, 0.20m, new DateOnly(2023, 6, 1), "year-2023")
        };

        var result = DividendReliabilityEvaluator.Evaluate(
            events,
            [],
            new DateOnly(2026, 9, 1));

        Assert.Equal("cautious", result);
    }

    [Fact]
    public void Evaluate_returns_passed_when_history_and_financial_quality_are_sufficient()
    {
        var securityId = Guid.NewGuid();
        var events = Enumerable.Range(2021, 5)
            .Select(year => CreateDividendEvent(
                securityId,
                0.20m,
                new DateOnly(year, 6, 1),
                $"dividend-{year}"))
            .ToArray();
        var financialSnapshot = FinancialSnapshot.Create(
            securityId,
            new DateOnly(2025, 12, 31),
            new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 9, 8, 0, 0, TimeSpan.Zero),
            0.80m,
            0.45m,
            0.40m,
            0.90m,
            0.12m,
            "FTShare",
            "financial-2025",
            "valid");

        var result = DividendReliabilityEvaluator.Evaluate(
            events,
            [financialSnapshot],
            new DateOnly(2026, 9, 1));

        Assert.Equal("passed", result);
    }

    [Fact]
    public void Evaluate_returns_failed_when_recent_three_year_history_is_incomplete()
    {
        var securityId = Guid.NewGuid();
        var events = new[]
        {
            CreateDividendEvent(
                securityId,
                0.20m,
                new DateOnly(2024, 6, 1),
                "dividend-2024"),
            CreateDividendEvent(
                securityId,
                0.20m,
                new DateOnly(2023, 6, 1),
                "dividend-2023")
        };

        var result = DividendReliabilityEvaluator.Evaluate(
            events,
            [],
            new DateOnly(2026, 9, 1));

        Assert.Equal("failed", result);
    }

    [Fact]
    public void Evaluate_returns_re_evaluate_when_a_recent_dividend_was_cancelled()
    {
        var securityId = Guid.NewGuid();
        var cancelledEvent = DividendEvent.Create(
            securityId,
            0.20m,
            "regular_cash",
            "cancelled",
            new DateOnly(2026, 8, 1),
            null,
            null,
            false,
            new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero),
            "FTShare",
            "cancelled-2026",
            "valid");

        var result = DividendReliabilityEvaluator.Evaluate(
            [cancelledEvent],
            [],
            new DateOnly(2026, 9, 1));

        Assert.Equal("re_evaluate", result);
    }

    private static DividendEvent CreateDividendEvent(
        Guid securityId,
        decimal dividendPerShare,
        DateOnly exDividendDate,
        string sourceRecordId)
        => DividendEvent.Create(
            securityId,
            dividendPerShare,
            "regular_cash",
            "implemented",
            new DateOnly(exDividendDate.Year, 5, 1),
            exDividendDate,
            exDividendDate.AddDays(1),
            false,
            new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            "FTShare",
            sourceRecordId,
            "valid");
}
