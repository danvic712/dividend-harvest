using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
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
        var result = await setupAppService.InitializeAsync(request, cancellationToken);
        return Created("/api/setup/status", result);
    }
}
