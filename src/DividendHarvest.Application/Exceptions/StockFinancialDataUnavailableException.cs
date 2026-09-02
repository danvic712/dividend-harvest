namespace DividendHarvest.Application.Exceptions;

public sealed class StockFinancialDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : InvalidOperationException(
        $"股票 {securityCode} 的财务数据暂时不可用。",
        innerException);
