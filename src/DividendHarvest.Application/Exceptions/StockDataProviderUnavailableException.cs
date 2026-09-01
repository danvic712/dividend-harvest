namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataProviderUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
