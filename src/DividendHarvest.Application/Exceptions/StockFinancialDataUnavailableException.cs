namespace DividendHarvest.Application.Exceptions;

public sealed class StockFinancialDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : ApplicationExceptionBase(
        "stock_financial_data_unavailable",
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode
        },
        innerException);
