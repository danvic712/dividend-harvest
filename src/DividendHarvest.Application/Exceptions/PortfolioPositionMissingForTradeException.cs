namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("portfolio_position_missing_for_trade")]
public sealed class PortfolioPositionMissingForTradeException()
    : ApplicationExceptionBase();
