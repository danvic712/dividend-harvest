using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Contracts;

namespace DividendHarvest.Background;

internal sealed class DailyStockDataSyncRunner(
    IServiceScopeFactory serviceScopeFactory) : IDailyStockDataSyncRunner
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<StockDataSyncRunResult> RunAsync(
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var syncAppService = scope.ServiceProvider
                .GetRequiredService<IStockDailyDataSyncAppService>();
            return await syncAppService.SyncAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }
}
