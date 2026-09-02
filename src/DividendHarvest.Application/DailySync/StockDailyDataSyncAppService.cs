using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;

namespace DividendHarvest.Application.DailySync;

public sealed class StockDailyDataSyncAppService(
    IStockWatchlistAppService stockWatchlistAppService,
    IStockPriceObservationAppService stockPriceObservationAppService,
    IStockDividendEventAppService stockDividendEventAppService,
    IStockFinancialSnapshotAppService stockFinancialSnapshotAppService,
    TimeProvider timeProvider) : IStockDailyDataSyncAppService
{
    public async Task<StockDataSyncRunResult> SyncAsync(
        CancellationToken cancellationToken)
    {
        var watchlist = await stockWatchlistAppService.GetAsync(cancellationToken);
        var failures = new List<StockDataSyncFailure>();
        var completedStockCount = 0;

        foreach (var stock in watchlist)
        {
            var request = new SyncStockPriceRequest(
                stock.SecurityCode,
                stock.ExchangeCode);
            var stockFailuresBeforeSync = failures.Count;

            await TrySyncAsync(
                stock,
                "price",
                () => stockPriceObservationAppService.SyncAsync(
                    request,
                    cancellationToken),
                failures,
                cancellationToken);
            await TrySyncAsync(
                stock,
                "dividend",
                () => stockDividendEventAppService.SyncAsync(
                    new SyncStockDividendsRequest(
                        stock.SecurityCode,
                        stock.ExchangeCode),
                    cancellationToken),
                failures,
                cancellationToken);
            await TrySyncAsync(
                stock,
                "financial",
                () => stockFinancialSnapshotAppService.SyncAsync(
                    new SyncStockFinancialsRequest(
                        stock.SecurityCode,
                        stock.ExchangeCode),
                    cancellationToken),
                failures,
                cancellationToken);

            if (failures.Count == stockFailuresBeforeSync)
            {
                completedStockCount++;
            }
        }

        return new StockDataSyncRunResult(
            watchlist.Count,
            completedStockCount,
            failures
                .Select(failure => (failure.SecurityCode, failure.ExchangeCode))
                .Distinct()
                .Count(),
            failures,
            timeProvider.GetUtcNow());
    }

    private static async Task TrySyncAsync(
        StockWatchlistItem stock,
        string dataKind,
        Func<Task> sync,
        ICollection<StockDataSyncFailure> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            await sync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsExpectedSyncFailure(exception))
        {
            failures.Add(new StockDataSyncFailure(
                stock.SecurityCode,
                stock.ExchangeCode,
                dataKind,
                exception.Message));
        }
    }

    private static bool IsExpectedSyncFailure(Exception exception)
        => exception is StockMarketDataUnavailableException
            or StockDividendDataUnavailableException
            or StockFinancialDataUnavailableException
            or StockDataSyncValidationException
            or StockNotConfiguredException;
}
