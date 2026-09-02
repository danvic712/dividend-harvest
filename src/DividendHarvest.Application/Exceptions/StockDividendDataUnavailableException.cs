namespace DividendHarvest.Application.Exceptions;

public sealed class StockDividendDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : ApplicationExceptionBase(
        "stock_dividend_data_unavailable",
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode
        },
        innerException);
