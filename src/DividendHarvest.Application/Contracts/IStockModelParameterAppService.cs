using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IStockModelParameterAppService
{
    Task<StockModelParameterSet?> GetAsync(
        GetStockModelParametersRequest request,
        CancellationToken cancellationToken);

    Task<StockModelParameterSet> SaveAsync(
        SaveStockModelParametersRequest request,
        CancellationToken cancellationToken);
}
