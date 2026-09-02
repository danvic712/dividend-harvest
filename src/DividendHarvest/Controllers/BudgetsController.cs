using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.Controllers;

[ApiController]
[Route("api/budgets")]
public sealed class BudgetsController(IBudgetAppService budgetAppService)
    : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<BudgetSummary>> GetSummary(
        CancellationToken cancellationToken)
    {
        var summary = await budgetAppService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpPost("entries")]
    public async Task<ActionResult<CashLedgerEntryResult>> RecordEntry(
        [FromBody] RecordCashLedgerEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await budgetAppService.RecordAsync(request, cancellationToken);
        return Created($"/api/budgets/entries/{result.CashLedgerEntryId}", result);
    }
}
