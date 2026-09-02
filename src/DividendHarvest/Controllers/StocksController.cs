using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController(IStockWatchlistAppService stockWatchlistAppService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockWatchlistItem>>> GetStocks(
        CancellationToken cancellationToken)
    {
        var stocks = await stockWatchlistAppService.GetAsync(cancellationToken);
        return Ok(stocks);
    }
}
