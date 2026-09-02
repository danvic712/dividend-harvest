using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class TtmDividendCalculatorTests
{
    [Fact]
    public void Calculate_ignores_a_dividend_published_after_the_as_of_date()
    {
        var dividendEvent = DividendEvent.Create(
            Guid.NewGuid(),
            0.80m,
            "regular_cash",
            "implemented",
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 8, 20),
            false,
            new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero),
            "FTShare",
            "future-dividend",
            "valid");

        var result = TtmDividendCalculator.Calculate(
            [dividendEvent],
            new DateOnly(2026, 9, 1));

        Assert.Null(result);
    }
}
