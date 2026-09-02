using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController(
    IStockWatchlistAppService stockWatchlistAppService,
    IStockModelParameterAppService stockModelParameterAppService,
    IStockPriceObservationAppService stockPriceObservationAppService,
    IStockDividendEventAppService stockDividendEventAppService,
    IStockAnalysisAppService stockAnalysisAppService,
    IStockFinancialSnapshotAppService stockFinancialSnapshotAppService)
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

    [HttpPost("{securityCode}/{exchangeCode}/price-observations/sync")]
    public async Task<ActionResult<StockPriceObservationResult>> SyncPriceObservation(
        string securityCode,
        string exchangeCode,
        CancellationToken cancellationToken)
    {
        var result = await stockPriceObservationAppService.SyncAsync(
            new SyncStockPriceRequest(securityCode, exchangeCode),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{securityCode}/{exchangeCode}/dividend-events/sync")]
    public async Task<ActionResult<IReadOnlyList<StockDividendEventResult>>> SyncDividendEvents(
        string securityCode,
        string exchangeCode,
        CancellationToken cancellationToken)
    {
        var result = await stockDividendEventAppService.SyncAsync(
            new SyncStockDividendsRequest(securityCode, exchangeCode),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{securityCode}/{exchangeCode}/financial-snapshots/sync")]
    public async Task<ActionResult<IReadOnlyList<StockFinancialSnapshotResult>>>
        SyncFinancialSnapshots(
            string securityCode,
            string exchangeCode,
            CancellationToken cancellationToken)
    {
        var result = await stockFinancialSnapshotAppService.SyncAsync(
            new SyncStockFinancialsRequest(securityCode, exchangeCode),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{securityCode}/{exchangeCode}/analysis")]
    public async Task<ActionResult<StockAnalysisResult>> GetAnalysis(
        string securityCode,
        string exchangeCode,
        CancellationToken cancellationToken)
    {
        var result = await stockAnalysisAppService.GetAsync(
            new GetStockAnalysisRequest(securityCode, exchangeCode),
            cancellationToken);
        return Ok(result);
    }
}
