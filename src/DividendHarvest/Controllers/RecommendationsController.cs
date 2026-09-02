using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController(
    IPortfolioRecommendationAppService portfolioRecommendationAppService,
    IRecommendationSnapshotAppService recommendationSnapshotAppService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PortfolioRecommendationResult>> Get(
        CancellationToken cancellationToken)
    {
        var result = await portfolioRecommendationAppService.GetAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("snapshots")]
    public async Task<ActionResult<CreateRecommendationSnapshotResult>> CreateSnapshot(
        CancellationToken cancellationToken)
    {
        var result = await recommendationSnapshotAppService.CreateAsync(cancellationToken);
        return Ok(result);
    }
}
