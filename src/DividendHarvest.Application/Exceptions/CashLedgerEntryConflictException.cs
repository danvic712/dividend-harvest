namespace DividendHarvest.Application.Exceptions;

public sealed class CashLedgerEntryConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        "cash_ledger_entry_conflict",
        $"现金流水来源记录标识 {sourceRecordId} 已被其他流水使用。");
