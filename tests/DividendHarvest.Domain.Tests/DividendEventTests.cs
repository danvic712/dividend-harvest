using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class DividendEventTests
{
    [Fact]
    public void Create_rejects_unknown_dividend_status_code()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            DividendEvent.Create(
                Guid.NewGuid(),
                0.31m,
                "regular_cash",
                "unknown",
                null,
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 7, 2),
                false,
                new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
                "FTShare",
                "dividend-1",
                "valid"));

        Assert.Equal("dividendStatusCode", exception.ParamName);
    }
}
