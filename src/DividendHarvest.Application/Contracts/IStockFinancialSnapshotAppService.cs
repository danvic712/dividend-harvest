using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IStockFinancialSnapshotAppService
{
    Task<IReadOnlyList<StockFinancialSnapshotResult>> SyncAsync(
        SyncStockFinancialsRequest request,
        CancellationToken cancellationToken);
}
