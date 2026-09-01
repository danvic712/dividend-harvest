namespace DividendHarvest.Infrastructure.DataAccess.Entities;

public sealed class PortfolioEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
