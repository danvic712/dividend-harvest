namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("cash_ledger_entry_conflict")]
public sealed class CashLedgerEntryConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        "cash_ledger_entry_conflict",
        new Dictionary<string, object?>
        {
            ["sourceRecordId"] = sourceRecordId
        });
