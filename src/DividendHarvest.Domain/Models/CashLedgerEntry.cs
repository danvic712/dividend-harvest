using DividendHarvest.Domain.Portfolio;

namespace DividendHarvest.Domain.Models;

public sealed class CashLedgerEntry
{
    private CashLedgerEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Guid? SecurityId { get; private set; }

    public DateOnly EntryDate { get; private set; }

    public string EntryTypeCode { get; private set; } = string.Empty;

    public string CashDirectionCode { get; private set; } = string.Empty;

    public decimal CashAmount { get; private set; }

    public string? SourceRecordId { get; private set; }

    public static CashLedgerEntry Create(
        Guid portfolioId,
        Guid? securityId,
        DateOnly entryDate,
        string entryTypeCode,
        string cashDirectionCode,
        decimal cashAmount,
        string? sourceRecordId)
    {
        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException("投资组合标识不能为空。", nameof(portfolioId));
        }

        if (securityId == Guid.Empty)
        {
            throw new ArgumentException("股票标识不能为空。", nameof(securityId));
        }

        if (entryDate == DateOnly.MinValue)
        {
            throw new ArgumentException("现金流水日期不能为空。", nameof(entryDate));
        }

        var normalizedEntryType = entryTypeCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!CashLedgerCodes.IsSupportedEntryType(normalizedEntryType))
        {
            throw new ArgumentException("现金流水类型不受支持。", nameof(entryTypeCode));
        }

        var normalizedDirection = cashDirectionCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!CashLedgerCodes.IsSupportedDirection(normalizedDirection))
        {
            throw new ArgumentException("现金流水方向不受支持。", nameof(cashDirectionCode));
        }

        if (!CashLedgerCodes.IsCompatible(normalizedEntryType, normalizedDirection))
        {
            throw new ArgumentException("现金流水类型和方向不匹配。", nameof(cashDirectionCode));
        }

        if (cashAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cashAmount),
                cashAmount,
                "现金流水金额必须大于零。");
        }

        return new CashLedgerEntry
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            SecurityId = securityId,
            EntryDate = entryDate,
            EntryTypeCode = normalizedEntryType,
            CashDirectionCode = normalizedDirection,
            CashAmount = cashAmount,
            SourceRecordId = string.IsNullOrWhiteSpace(sourceRecordId)
                ? null
                : sourceRecordId.Trim()
        };
    }
}
