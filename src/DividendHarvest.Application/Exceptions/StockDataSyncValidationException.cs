namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataSyncValidationException(string message)
    : Exception(message);
