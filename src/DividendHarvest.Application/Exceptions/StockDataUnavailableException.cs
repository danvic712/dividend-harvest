namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_data_unavailable")]
public sealed class StockDataUnavailableException(string securityCode, Exception? innerException = null)
    : ApplicationExceptionBase(
        "stock_data_unavailable",
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode
        },
        innerException);
