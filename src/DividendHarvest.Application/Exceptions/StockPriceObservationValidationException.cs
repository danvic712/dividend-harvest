namespace DividendHarvest.Application.Exceptions;

public sealed class StockPriceObservationValidationException(string message)
    : Exception(message);
