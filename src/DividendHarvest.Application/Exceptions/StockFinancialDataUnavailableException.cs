namespace DividendHarvest.Application.Exceptions;

public sealed class StockFinancialDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : ApplicationExceptionBase(
        "stock_financial_data_unavailable",
        $"股票 {securityCode} 的财务数据暂时不可用。",
        innerException);
