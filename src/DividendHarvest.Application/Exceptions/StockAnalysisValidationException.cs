namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_analysis_validation_failed")]
public sealed class StockAnalysisValidationException(string message)
    : ApplicationValidationException("stock_analysis_validation_failed", message);
