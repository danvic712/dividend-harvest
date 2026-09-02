namespace DividendHarvest.Application.Exceptions;

public sealed class PortfolioTradeValidationException(string message)
    : InvalidOperationException(message);
