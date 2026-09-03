using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Diagnostics;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Contracts;

namespace DividendHarvest.Background;

internal sealed class StockDataSyncBackgroundService(
    StockDataSyncTaskQueue taskQueue,
    IDailyStockDataSyncRunner syncRunner,
    ILogger<StockDataSyncBackgroundService> logger,
    IDiagnosticContext diagnosticContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var _ in taskQueue.ReadAllAsync(stoppingToken))
            {
                await RunSyncAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
        using var diagnosticScope = diagnosticContext.BeginScope(new DiagnosticScope(
            "stock_data_sync",
            CorrelationId: runId,
            RunId: runId));

        try
        {
            var result = await syncRunner.RunAsync(cancellationToken);
            logger.LogInformation(
                "Background stock data synchronization finished. RunId: {RunId}, attempted: {Attempted}, completed: {Completed}, failed: {Failed}.",
                runId,
                result.AttemptedStockCount,
                result.FullyCompletedStockCount,
                result.PartiallyFailedStockCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (ApplicationExceptionBase exception)
        {
            var causeType = exception.InnerException?.GetType().Name ?? exception.GetType().Name;
            logger.LogError(
                "Background stock data synchronization failed. RunId: {RunId}, error code: {ErrorCode}, cause type: {CauseType}.",
                runId,
                exception.ErrorCode,
                causeType);
        }
        catch (Exception exception)
        {
            var causeType = exception.InnerException?.GetType().Name ?? exception.GetType().Name;
            logger.LogError(
                "Background stock data synchronization failed. RunId: {RunId}, exception type: {ExceptionType}, cause type: {CauseType}.",
                runId,
                exception.GetType().Name,
                causeType);
        }
    }
}
