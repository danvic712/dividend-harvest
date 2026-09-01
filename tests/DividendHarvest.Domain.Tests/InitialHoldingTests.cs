using DividendHarvest.Domain.Portfolio;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class InitialHoldingTests
{
    [Fact]
    public void Create_accepts_a_valid_initial_holding()
    {
        var holding = InitialHolding.Create(
            heldShares: 100,
            coreShares: 60,
            targetShares: 200,
            averageCostPerShare: 10.25m);

        Assert.Equal(100, holding.HeldShares);
        Assert.Equal(60, holding.CoreShares);
        Assert.Equal(200, holding.TargetShares);
        Assert.Equal(10.25m, holding.AverageCostPerShare);
    }

    [Fact]
    public void Create_rejects_core_shares_above_held_shares()
    {
        Assert.Throws<ArgumentException>(() => InitialHolding.Create(100, 101, 200, 10m));
    }
}
