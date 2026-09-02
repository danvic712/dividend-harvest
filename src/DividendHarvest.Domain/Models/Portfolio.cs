namespace DividendHarvest.Domain.Models;

public sealed class Portfolio
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "CNY";
}
