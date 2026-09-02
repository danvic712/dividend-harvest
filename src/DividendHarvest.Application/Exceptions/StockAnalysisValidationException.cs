namespace DividendHarvest.Application.Exceptions;

public sealed class StockAnalysisValidationException(string message)
    : ApplicationValidationException("stock_analysis_validation_failed", message);
