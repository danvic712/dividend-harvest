namespace DividendHarvest.Application.Exceptions;

public sealed class StockNotConfiguredException(string securityCode, string exchangeCode)
    : ApplicationExceptionBase(
        "stock_not_configured",
        $"股票 {securityCode}（{exchangeCode}）尚未配置到关注列表。");
