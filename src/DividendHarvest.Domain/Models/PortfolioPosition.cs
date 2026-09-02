namespace DividendHarvest.Domain.Models;

public sealed class PortfolioPosition
{
    public Guid PortfolioId { get; set; }

    public Guid SecurityId { get; set; }

    public int HeldShares { get; set; }

    public int CoreShares { get; set; }

    public int TargetShares { get; set; }

    public decimal AverageCostPerShare { get; set; }
}
