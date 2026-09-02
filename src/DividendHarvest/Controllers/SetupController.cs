using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/setup")]
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
        return CreatedAtAction(
            nameof(GetStatus),
            new { version = "1" },
            result);
    }
}
