using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IStockRecommendationAppService
{
    Task<StockRecommendationResult> GetAsync(
        GetStockAnalysisRequest request,
        CancellationToken cancellationToken);
}
