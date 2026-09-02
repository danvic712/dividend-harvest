using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IStockDailyDataSyncAppService
{
    Task<StockDataSyncRunResult> SyncAsync(CancellationToken cancellationToken);
}
