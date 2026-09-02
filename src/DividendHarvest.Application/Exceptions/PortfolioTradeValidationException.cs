namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("portfolio_trade_validation_failed")]
public sealed class PortfolioTradeValidationException(string message)
    : ApplicationValidationException(message);
