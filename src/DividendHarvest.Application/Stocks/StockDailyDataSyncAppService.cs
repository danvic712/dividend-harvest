using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;

namespace DividendHarvest.Application.Stocks;

public sealed class StockDailyDataSyncAppService(
    IStockWatchlistAppService stockWatchlistAppService,
    IStockPriceObservationAppService stockPriceObservationAppService,
    IStockDividendEventAppService stockDividendEventAppService,
    IStockFinancialSnapshotAppService stockFinancialSnapshotAppService,
    IApplicationErrorCatalog applicationErrorCatalog,
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

    private async Task TrySyncAsync(
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
            var applicationException = (ApplicationExceptionBase)exception;
            var localizedError = applicationErrorCatalog.Resolve(applicationException);
            failures.Add(new StockDataSyncFailure(
                stock.SecurityCode,
                stock.ExchangeCode,
                dataKind,
                applicationException.ErrorCode,
                localizedError.Detail));
        }
    }

    private static bool IsExpectedSyncFailure(Exception exception)
        => exception is StockMarketDataUnavailableException
            or StockDividendDataUnavailableException
            or StockFinancialDataUnavailableException
            or StockDataSyncValidationException
            or StockNotConfiguredException;
}
