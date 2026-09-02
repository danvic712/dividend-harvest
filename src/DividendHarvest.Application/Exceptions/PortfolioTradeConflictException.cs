namespace DividendHarvest.Application.Exceptions;

public sealed class PortfolioTradeConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        "portfolio_trade_conflict",
        new Dictionary<string, object?>
        {
            ["sourceRecordId"] = sourceRecordId
        });
