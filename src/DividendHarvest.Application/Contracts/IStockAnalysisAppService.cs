using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IStockAnalysisAppService
{
    Task<StockAnalysisResult> GetAsync(
        GetStockAnalysisRequest request,
        CancellationToken cancellationToken);
}
