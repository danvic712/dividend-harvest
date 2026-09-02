namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_financial_data_unavailable")]
public sealed class StockFinancialDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : ApplicationExceptionBase(
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode
        },
        innerException);
