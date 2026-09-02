using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class CashLedgerEntryTests
{
    [Fact]
    public void Create_normalizes_codes_and_optional_source_record()
    {
        var result = CashLedgerEntry.Create(
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 9, 1),
            " BUDGET_DEPOSIT ",
            " INFLOW ",
            1000m,
            "  manual-1  ");

        Assert.Equal("budget_deposit", result.EntryTypeCode);
        Assert.Equal("inflow", result.CashDirectionCode);
        Assert.Equal("manual-1", result.SourceRecordId);
        Assert.Equal(1000m, result.CashAmount);
    }

    [Fact]
    public void Create_rejects_incompatible_entry_type_and_direction()
    {
        Assert.Throws<ArgumentException>(() => CashLedgerEntry.Create(
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 9, 1),
            "buy",
            "inflow",
            1000m,
            null));
    }
}
