namespace DividendHarvest.Application.Exceptions;

public sealed class PortfolioTradeConflictException(string sourceRecordId)
    : ApplicationExceptionBase(
        "portfolio_trade_conflict",
        $"交易来源记录标识 {sourceRecordId} 已被其他交易使用。");
