namespace DividendHarvest.Infrastructure.DataAccess.Entities;

public sealed class PortfolioPositionEntity
{
    public Guid PortfolioId { get; set; }

    public Guid SecurityId { get; set; }

    public int HeldShares { get; set; }

    public int CoreShares { get; set; }

    public int TargetShares { get; set; }

    public decimal AverageCostPerShare { get; set; }
}
