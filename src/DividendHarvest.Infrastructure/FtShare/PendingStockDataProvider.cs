using DividendHarvest.Application.Ports;
using DividendHarvest.Domain.Securities;

namespace DividendHarvest.Infrastructure.FtShare;

public sealed class PendingStockDataProvider : IStockDataProvider
{
    public Task<StockData?> GetAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
        => Task.FromResult<StockData?>(null);
}
