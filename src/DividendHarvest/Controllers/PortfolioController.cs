using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/portfolio")]
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
