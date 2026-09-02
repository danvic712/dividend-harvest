namespace DividendHarvest.Application.Exceptions;

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
