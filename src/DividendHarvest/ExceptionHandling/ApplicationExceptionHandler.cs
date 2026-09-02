using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.ExceptionHandling;

public sealed class ApplicationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IApplicationErrorCatalog errorCatalog,
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

        logger.LogWarning(
            "Application request failed with status code {StatusCode}, error code {ErrorCode}, and locale {Locale}.",
            localizedError.StatusCode,
            localizedError.ErrorCode,
            localizedError.CultureName);

        httpContext.Response.StatusCode = localizedError.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = localizedError.StatusCode,
                Title = localizedError.Title,
                Detail = localizedError.Detail,
                Extensions =
                {
                    ["error_code"] = localizedError.ErrorCode,
                    ["locale"] = localizedError.CultureName
                }
            }
        });
    }
}
