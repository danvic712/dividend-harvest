namespace DividendHarvest.Application.Exceptions;

public sealed class StockNotConfiguredException(string securityCode, string exchangeCode)
    : KeyNotFoundException($"股票 {securityCode}（{exchangeCode}）尚未配置到关注列表。");
