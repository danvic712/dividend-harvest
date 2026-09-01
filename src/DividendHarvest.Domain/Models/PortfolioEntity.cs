namespace DividendHarvest.Domain.Models;

public sealed class PortfolioEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
