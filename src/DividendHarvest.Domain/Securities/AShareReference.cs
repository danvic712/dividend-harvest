namespace DividendHarvest.Domain.Securities;

public sealed record AShareReference
{
    private static readonly string[] SupportedExchanges = ["SSE", "SZSE", "BSE"];

    private AShareReference(string securityCode, string exchangeCode)
    {
        SecurityCode = securityCode;
        ExchangeCode = exchangeCode;
    }

    public string SecurityCode { get; }

    public string ExchangeCode { get; }

    public static AShareReference Create(string securityCode, string exchangeCode)
    {
        var normalizedCode = securityCode?.Trim() ?? string.Empty;
        var normalizedExchange = exchangeCode?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalizedCode.Length != 6 || normalizedCode.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException("A 股股票代码必须是 6 位数字。", nameof(securityCode));
        }

        if (!SupportedExchanges.Contains(normalizedExchange, StringComparer.Ordinal))
        {
            throw new ArgumentException("交易所必须是 SSE、SZSE 或 BSE。", nameof(exchangeCode));
        }

        return new AShareReference(normalizedCode, normalizedExchange);
    }
}
