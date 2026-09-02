namespace DividendHarvest.Application.Exceptions;

public sealed class PortfolioTradeValidationException(string message)
    : ApplicationValidationException("portfolio_trade_validation_failed", message);
