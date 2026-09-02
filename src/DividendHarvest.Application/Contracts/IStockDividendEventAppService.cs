using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IStockDividendEventAppService
{
    Task<IReadOnlyList<StockDividendEventResult>> SyncAsync(
        SyncStockDividendsRequest request,
        CancellationToken cancellationToken);
}
