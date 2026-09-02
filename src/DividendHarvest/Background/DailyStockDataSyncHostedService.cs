using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Configuration;
using Microsoft.Extensions.Options;

namespace DividendHarvest.Background;

public sealed class DailyStockDataSyncHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<DailySyncOptions> options,
    TimeProvider timeProvider,
    ILogger<DailyStockDataSyncHostedService> logger) : BackgroundService
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
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var syncAppService = scope.ServiceProvider
                .GetRequiredService<IStockDailyDataSyncAppService>();
            var result = await syncAppService.SyncAsync(cancellationToken);
            logger.LogInformation(
                "Daily stock data synchronization finished. Attempted: {Attempted}, completed: {Completed}, failed: {Failed}.",
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
            logger.LogError(
                "Daily stock data synchronization failed. Error code: {ErrorCode}.",
                exception.ErrorCode);
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Daily stock data synchronization failed with exception type {ExceptionType}.",
                exception.GetType().Name);
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
