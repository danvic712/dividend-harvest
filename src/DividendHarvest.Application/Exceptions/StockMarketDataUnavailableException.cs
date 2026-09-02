namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_market_data_unavailable")]
public sealed class StockMarketDataUnavailableException(
    string securityCode,
    Exception? innerException = null)
    : ApplicationExceptionBase(
        "stock_market_data_unavailable",
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode
        },
        innerException);
