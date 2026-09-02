using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController(
    IPortfolioRecommendationAppService portfolioRecommendationAppService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PortfolioRecommendationResult>> Get(
        CancellationToken cancellationToken)
    {
        var result = await portfolioRecommendationAppService.GetAsync(cancellationToken);
        return Ok(result);
    }
}
