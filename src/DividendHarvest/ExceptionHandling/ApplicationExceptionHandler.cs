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
            StockDataSyncValidationException =>
                (StatusCodes.Status400BadRequest, "股票同步请求无效"),
            StockAnalysisValidationException =>
                (StatusCodes.Status400BadRequest, "股票分析请求无效"),
            BudgetValidationException =>
                (StatusCodes.Status400BadRequest, "预算流水请求无效"),
            PortfolioTradeValidationException =>
                (StatusCodes.Status400BadRequest, "交易记录无效"),
            SetupAlreadyCompletedException => (StatusCodes.Status409Conflict, "系统已经完成建账"),
            SetupNotCompletedException => (StatusCodes.Status409Conflict, "系统尚未完成建账"),
            ModelParameterVersionAlreadyExistsException =>
                (StatusCodes.Status409Conflict, "股票模型参数版本已存在"),
            CashLedgerEntryConflictException =>
                (StatusCodes.Status409Conflict, "现金流水来源记录已存在冲突"),
            PortfolioTradeConflictException =>
                (StatusCodes.Status409Conflict, "交易来源记录已存在冲突"),
            StockNotConfiguredException => (StatusCodes.Status404NotFound, "股票尚未配置"),
            StockMarketDataUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "股票行情数据暂时不可用"),
            StockDividendDataUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "股票股息数据暂时不可用"),
            StockFinancialDataUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "股票财务数据暂时不可用"),
            StockDataProviderUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "股票数据提供方暂时不可用"),
            StockDataUnavailableException => (StatusCodes.Status503ServiceUnavailable, "股票基础资料不可用"),
            _ => ((int StatusCode, string Title)?)null
        };

        if (mapping is null)
        {
            return false;
        }

        logger.LogWarning(
            "Application request failed with status code {StatusCode} and error code {ErrorCode}.",
            mapping.Value.StatusCode,
            (exception as ApplicationExceptionBase)?.ErrorCode);

        httpContext.Response.StatusCode = mapping.Value.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = mapping.Value.StatusCode,
                Title = mapping.Value.Title,
                Detail = GetPublicDetail(exception, mapping.Value.Title),
                Extensions =
                {
                    ["error_code"] = (exception as ApplicationExceptionBase)?.ErrorCode
                }
            }
        });
    }

    private static string GetPublicDetail(Exception exception, string fallback)
        => exception is ApplicationValidationException
            or SetupAlreadyCompletedException
            or SetupNotCompletedException
            or ModelParameterVersionAlreadyExistsException
            or CashLedgerEntryConflictException
            or PortfolioTradeConflictException
            or StockNotConfiguredException
            ? exception.Message
            : fallback;
}
