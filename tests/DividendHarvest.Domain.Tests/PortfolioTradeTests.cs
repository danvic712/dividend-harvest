using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class PortfolioTradeTests
{
    [Fact]
    public void Create_normalizes_direction_and_optional_source_record()
    {
        var result = PortfolioTrade.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            " BUY ",
            100,
            4m,
            5m,
            " trade-1 ");

        Assert.Equal("buy", result.TradeDirectionCode);
        Assert.Equal(100, result.ShareQuantity);
        Assert.Equal("trade-1", result.SourceRecordId);
    }

    [Fact]
    public void Create_rejects_zero_share_quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PortfolioTrade.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            "buy",
            0,
            4m,
            0m,
            null));
    }

    [Fact]
    public void Position_buy_updates_weighted_average_cost_including_fee()
    {
        var position = new PortfolioPosition
        {
            HeldShares = 100,
            CoreShares = 60,
            AverageCostPerShare = 3m
        };

        position.ApplyBuy(100, 5m, 10m);

        Assert.Equal(200, position.HeldShares);
        Assert.Equal(4.05m, position.AverageCostPerShare);
    }

    [Fact]
    public void Position_sell_rejects_breaking_the_core_position()
    {
        var position = new PortfolioPosition
        {
            HeldShares = 100,
            CoreShares = 60,
            AverageCostPerShare = 3m
        };

        Assert.Throws<InvalidOperationException>(() => position.ApplySell(50));
    }
}
