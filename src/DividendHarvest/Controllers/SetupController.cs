using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/setup")]
public sealed class SetupController(ISetupAppService setupAppService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SetupStatus>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await setupAppService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost]
    public async Task<ActionResult<SetupResult>> Initialize(
        [FromBody] SetupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await setupAppService.InitializeAsync(request, cancellationToken);
            return Created("/api/setup/status", result);
        }
        catch (SetupValidationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "建账请求无效");
        }
        catch (SetupAlreadyCompletedException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict,
                title: "系统已经完成建账");
        }
        catch (StockDataUnavailableException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "股票基础资料不可用");
        }
    }
}
