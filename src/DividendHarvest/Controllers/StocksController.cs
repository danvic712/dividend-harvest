using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController(
    IStockWatchlistAppService stockWatchlistAppService,
    IStockModelParameterAppService stockModelParameterAppService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockWatchlistItem>>> GetStocks(
        CancellationToken cancellationToken)
    {
        var stocks = await stockWatchlistAppService.GetAsync(cancellationToken);
        return Ok(stocks);
    }

    [HttpGet("{securityCode}/{exchangeCode}/model-parameters")]
    public async Task<ActionResult<StockModelParameterSet>> GetModelParameters(
        string securityCode,
        string exchangeCode,
        CancellationToken cancellationToken)
    {
        var parameters = await stockModelParameterAppService.GetAsync(
            new GetStockModelParametersRequest(securityCode, exchangeCode),
            cancellationToken);
        return parameters is null ? NotFound() : Ok(parameters);
    }

    [HttpPost("model-parameters")]
    public async Task<ActionResult<StockModelParameterSet>> SaveModelParameters(
        [FromBody] SaveStockModelParametersRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = await stockModelParameterAppService.SaveAsync(
            request,
            cancellationToken);
        return CreatedAtAction(
            nameof(GetModelParameters),
            new
            {
                securityCode = parameters.SecurityCode,
                exchangeCode = parameters.ExchangeCode
            },
            parameters);
    }
}
