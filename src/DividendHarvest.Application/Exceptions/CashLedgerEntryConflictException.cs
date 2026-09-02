namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("cash_ledger_entry_conflict")]
public sealed class CashLedgerEntryConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        new Dictionary<string, object?>
        {
            ["sourceRecordId"] = sourceRecordId
        });
