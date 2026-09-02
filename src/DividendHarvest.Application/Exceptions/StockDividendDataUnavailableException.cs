namespace DividendHarvest.Application.Exceptions;

public sealed class StockDividendDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : InvalidOperationException(
        $"股票 {securityCode} 的股息数据暂时不可用。",
        innerException);
