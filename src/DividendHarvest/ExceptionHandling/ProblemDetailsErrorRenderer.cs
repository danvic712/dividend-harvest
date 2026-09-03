using DividendHarvest.Contracts;
using DividendHarvest.Application.Localization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.ExceptionHandling;

public sealed class ProblemDetailsErrorRenderer(
    IProblemDetailsService problemDetailsService) : IHttpErrorRenderer
{
    public async ValueTask<bool> RenderAsync(
        HttpContext httpContext,
        LocalizedApplicationError error,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = error.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = error.StatusCode,
                Title = error.Title,
                Detail = error.Detail,
                Extensions =
                {
                    ["error_code"] = error.ErrorCode,
                    ["locale"] = error.CultureName,
                    ["trace_id"] = httpContext.TraceIdentifier
                }
            }
        });
    }
}
