using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Diagnostics;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Configuration;
using DividendHarvest.Contracts;
using Microsoft.Extensions.Options;

namespace DividendHarvest.Background;

public sealed class DailyStockDataSyncHostedService(
    IDailyStockDataSyncRunner syncRunner,
    IOptions<DailySyncOptions> options,
    TimeProvider timeProvider,
    ILogger<DailyStockDataSyncHostedService> logger,
    IDiagnosticContext diagnosticContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Daily stock data synchronization is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var timeZone = ResolveTimeZone(options.Value.TimeZoneId);
            var localTime = TimeOnly.TryParse(options.Value.LocalTime, out var parsedTime)
                ? parsedTime
                : new TimeOnly(18, 0);
            var nextRun = DailySyncSchedule.GetNextRunUtc(
                timeProvider.GetUtcNow(),
                localTime,
                timeZone);
            var delay = nextRun - timeProvider.GetUtcNow();

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }

            await RunSyncAsync(stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
        using var diagnosticScope = diagnosticContext.BeginScope(new DiagnosticScope(
            "daily_stock_data_sync",
            CorrelationId: runId,
            RunId: runId));

        try
        {
            var result = await syncRunner.RunAsync(cancellationToken);
            logger.LogInformation(
                "Daily stock data synchronization finished. RunId: {RunId}, attempted: {Attempted}, completed: {Completed}, failed: {Failed}.",
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
                "Daily stock data synchronization failed. RunId: {RunId}, error code: {ErrorCode}, cause type: {CauseType}.",
                runId,
                exception.ErrorCode,
                causeType);
        }
        catch (Exception exception)
        {
            var causeType = exception.InnerException?.GetType().Name ?? exception.GetType().Name;
            logger.LogError(
                "Daily stock data synchronization failed. RunId: {RunId}, exception type: {ExceptionType}, cause type: {CauseType}.",
                runId,
                exception.GetType().Name,
                causeType);
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
