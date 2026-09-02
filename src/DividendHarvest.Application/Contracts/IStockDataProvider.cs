using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Securities;

namespace DividendHarvest.Application.Contracts;

public interface IStockDataProvider
{
    Task<StockData?> GetAsync(
        AShareReference reference,
        CancellationToken cancellationToken);
}
