namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataUnavailableException(string securityCode, Exception? innerException = null)
    : InvalidOperationException($"股票 {securityCode} 的基础资料暂时不可用。", innerException);
