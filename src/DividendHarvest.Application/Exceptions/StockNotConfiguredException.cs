namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("stock_not_configured")]
public sealed class StockNotConfiguredException(string securityCode, string exchangeCode)
    : ApplicationExceptionBase(
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode,
            ["exchangeCode"] = exchangeCode
        });
