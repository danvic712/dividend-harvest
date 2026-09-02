namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("portfolio_trade_conflict")]
public sealed class PortfolioTradeConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        "portfolio_trade_conflict",
        new Dictionary<string, object?>
        {
            ["sourceRecordId"] = sourceRecordId
        });
