using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IBudgetAppService
{
    Task<CashLedgerEntryResult> RecordAsync(
        RecordCashLedgerEntryRequest request,
        CancellationToken cancellationToken);

    Task<BudgetSummary> GetSummaryAsync(CancellationToken cancellationToken);
}
