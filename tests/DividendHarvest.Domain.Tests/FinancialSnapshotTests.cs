using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class FinancialSnapshotTests
{
    [Fact]
    public void Create_rejects_an_empty_data_source()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FinancialSnapshot.Create(
                Guid.NewGuid(),
                new DateOnly(2026, 6, 30),
                new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
                null,
                1.20m,
                0.45m,
                0.40m,
                0.90m,
                0.12m,
                "",
                "financial-1",
                "valid"));

        Assert.Equal("dataSource", exception.ParamName);
    }
}
