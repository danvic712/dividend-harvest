using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Contracts;

public interface IDailyStockDataSyncRunner
{
    Task<StockDataSyncRunResult> RunAsync(CancellationToken cancellationToken);
}
