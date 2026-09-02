using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IPortfolioRecommendationAppService
{
    Task<PortfolioRecommendationResult> GetAsync(
        CancellationToken cancellationToken);
}
