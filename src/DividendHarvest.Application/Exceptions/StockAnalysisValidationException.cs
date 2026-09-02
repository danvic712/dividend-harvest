namespace DividendHarvest.Application.Exceptions;

public sealed class StockAnalysisValidationException(string message)
    : Exception(message);
