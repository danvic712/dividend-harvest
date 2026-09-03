using System.Threading.Channels;
using DividendHarvest.Application.Contracts;

namespace DividendHarvest.Background;

internal sealed class StockDataSyncTaskQueue : IStockDataSyncScheduler
{
    private readonly Channel<bool> channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TrySchedule()
        => channel.Writer.TryWrite(true);

    public IAsyncEnumerable<bool> ReadAllAsync(CancellationToken cancellationToken)
        => channel.Reader.ReadAllAsync(cancellationToken);
}
