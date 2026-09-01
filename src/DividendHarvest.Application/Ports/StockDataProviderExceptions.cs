namespace DividendHarvest.Application.Ports;

public sealed class StockDataProviderUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
