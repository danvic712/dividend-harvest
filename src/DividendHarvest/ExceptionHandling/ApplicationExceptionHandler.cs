using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Diagnostics;
using DividendHarvest.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using DividendHarvest.Contracts;

namespace DividendHarvest.ExceptionHandling;

public sealed class ApplicationExceptionHandler(
    IApplicationErrorCatalog errorCatalog,
    IDiagnosticContext diagnosticContext,
    IHttpErrorRenderer errorRenderer,
    ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApplicationExceptionBase applicationException)
        {
            return false;
        }

        var localizedError = errorCatalog.Resolve(
            applicationException,
            httpContext.Request.Headers.AcceptLanguage.ToString());

        using var diagnosticScope = diagnosticContext.BeginScope(new DiagnosticScope(
            "http_error",
            CorrelationId: httpContext.TraceIdentifier,
            ErrorCode: localizedError.ErrorCode,
            Severity: "warning"));
        var causeType = exception.InnerException?.GetType().Name ?? exception.GetType().Name;
        logger.LogWarning(
            "Application request failed with status code {StatusCode}, error code {ErrorCode}, locale {Locale}, and cause type {CauseType}.",
            localizedError.StatusCode,
            localizedError.ErrorCode,
            localizedError.CultureName,
            causeType);

        return await errorRenderer.RenderAsync(
            httpContext,
            localizedError,
            cancellationToken);
    }
}
