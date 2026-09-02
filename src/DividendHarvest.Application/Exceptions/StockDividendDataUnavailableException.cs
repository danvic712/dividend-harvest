namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_dividend_data_unavailable")]
public sealed class StockDividendDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : ApplicationExceptionBase(
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode
        },
        innerException);
