namespace DividendHarvest.Application.Exceptions;

public sealed class CashLedgerEntryConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        "cash_ledger_entry_conflict",
        new Dictionary<string, object?>
        {
            ["sourceRecordId"] = sourceRecordId
        });
