namespace DividendHarvest.Application.Exceptions;

public sealed class StockDataUnavailableException(string securityCode, Exception? innerException = null)
    : ApplicationExceptionBase(
        "stock_data_unavailable",
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode
        },
        innerException);
