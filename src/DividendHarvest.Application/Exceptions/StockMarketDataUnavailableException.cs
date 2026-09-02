namespace DividendHarvest.Application.Exceptions;

public sealed class StockMarketDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : InvalidOperationException(
        $"股票 {securityCode} 的行情数据暂时不可用。",
        innerException);
