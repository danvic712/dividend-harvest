using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Securities;

namespace DividendHarvest.Application.Stocks;

public sealed class StockDailyDataSyncAppService(
    IStockWatchlistAppService stockWatchlistAppService,
    IStockFactSyncAppService stockFactSyncAppService,
    TimeProvider timeProvider) : IStockDailyDataSyncAppService
{
    public async Task<StockDataSyncRunResult> SyncAsync(
        CancellationToken cancellationToken)
    {
        var watchlist = await stockWatchlistAppService.GetAsync(cancellationToken);
        var failures = new List<StockDataSyncFailure>();
        var fullyCompletedStockCount = 0;

        foreach (var stock in watchlist)
        {
            var result = await stockFactSyncAppService.SyncAsync(
                AShareReference.Create(stock.SecurityCode, stock.ExchangeCode),
                cancellationToken);
            failures.AddRange(result.Failures);

            if (result.Failures.Count == 0)
            {
                fullyCompletedStockCount++;
            }
        }

        return new StockDataSyncRunResult(
            watchlist.Count,
            fullyCompletedStockCount,
            failures
                .Select(failure => (failure.SecurityCode, failure.ExchangeCode))
                .Distinct()
                .Count(),
            failures,
            timeProvider.GetUtcNow());
    }

}
