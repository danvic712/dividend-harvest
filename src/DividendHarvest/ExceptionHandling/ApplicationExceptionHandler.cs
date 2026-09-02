using DividendHarvest.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DividendHarvest.ExceptionHandling;

public sealed class ApplicationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var mapping = exception switch
        {
            SetupValidationException => (StatusCodes.Status400BadRequest, "建账请求无效"),
            ModelParameterValidationException => (StatusCodes.Status400BadRequest, "股票模型参数无效"),
            StockPriceObservationValidationException =>
                (StatusCodes.Status400BadRequest, "股票行情同步请求无效"),
            SetupAlreadyCompletedException => (StatusCodes.Status409Conflict, "系统已经完成建账"),
            SetupNotCompletedException => (StatusCodes.Status409Conflict, "系统尚未完成建账"),
            ModelParameterVersionAlreadyExistsException =>
                (StatusCodes.Status409Conflict, "股票模型参数版本已存在"),
            StockNotConfiguredException => (StatusCodes.Status404NotFound, "股票尚未配置"),
            StockMarketDataUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "股票行情数据暂时不可用"),
            StockDataUnavailableException => (StatusCodes.Status503ServiceUnavailable, "股票基础资料不可用"),
            _ => ((int StatusCode, string Title)?)null
        };

        if (mapping is null)
        {
            return false;
        }

        logger.LogWarning(
            "Application request failed with status code {StatusCode}.",
            mapping.Value.StatusCode);

        httpContext.Response.StatusCode = mapping.Value.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = mapping.Value.StatusCode,
                Title = mapping.Value.Title,
                Detail = exception.Message
            }
        });
    }
}
