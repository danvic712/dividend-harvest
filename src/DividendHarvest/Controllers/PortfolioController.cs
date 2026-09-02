using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/portfolio")]
public sealed class PortfolioController(IPortfolioTradeAppService portfolioTradeAppService)
    : ControllerBase
{
    [HttpPost("trades")]
    public async Task<ActionResult<PortfolioTradeResult>> RecordTrade(
        [FromBody] RecordPortfolioTradeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await portfolioTradeAppService.RecordAsync(request, cancellationToken);
        return Ok(result);
    }
}
