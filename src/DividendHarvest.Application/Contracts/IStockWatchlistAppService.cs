using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IStockWatchlistAppService
{
    Task<IReadOnlyList<StockWatchlistItem>> GetAsync(CancellationToken cancellationToken);
}
