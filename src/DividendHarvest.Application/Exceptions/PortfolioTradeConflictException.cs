namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("portfolio_trade_conflict")]
public sealed class PortfolioTradeConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        new Dictionary<string, object?>
        {
            ["sourceRecordId"] = sourceRecordId
        });
