using DividendHarvest.Application.Dtos;

namespace DividendHarvest.Application.Contracts;

public interface IRecommendationSnapshotAppService
{
    Task<CreateRecommendationSnapshotResult> CreateAsync(
        CancellationToken cancellationToken);
}
