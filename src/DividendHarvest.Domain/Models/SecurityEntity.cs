namespace DividendHarvest.Domain.Models;

public sealed class SecurityEntity
{
    public Guid Id { get; set; }

    public string SecurityCode { get; set; } = string.Empty;

    public string ExchangeCode { get; set; } = string.Empty;

    public string SecurityName { get; set; } = string.Empty;

    public string MarketCode { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;
}
