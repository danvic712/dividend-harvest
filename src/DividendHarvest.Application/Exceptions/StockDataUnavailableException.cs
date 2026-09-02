namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataUnavailableException(string securityCode, Exception? innerException = null)
    : ApplicationExceptionBase(
        "stock_data_unavailable",
        $"股票 {securityCode} 的基础资料暂时不可用。",
        innerException);
