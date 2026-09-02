namespace DividendHarvest.Application.Exceptions;

public sealed class StockNotConfiguredException(string securityCode, string exchangeCode)
    : ApplicationExceptionBase(
        "stock_not_configured",
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode,
            ["exchangeCode"] = exchangeCode
        });
