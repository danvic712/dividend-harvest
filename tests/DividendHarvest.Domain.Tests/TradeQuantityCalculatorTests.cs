using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Models;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class TradeQuantityCalculatorTests
{
    [Fact]
    public void Calculate_buy_uses_budget_limits_fee_and_trading_lot()
    {
        var parameters = CreateParameters();

        var result = TradeQuantityCalculator.Calculate(
            parameters,
            "available",
            "passed",
            "strong_buy",
            10m,
            100,
            60,
            500,
            5000m,
            10000m,
            2000m);

        Assert.Equal(200, result.SuggestedBuyShares);
        Assert.Equal(0, result.SuggestedSellShares);
        Assert.Equal(2000m, result.SuggestedTradeAmount);
        Assert.Equal(2m, result.EstimatedTransactionFeeAmount);
    }

    [Fact]
    public void Calculate_sell_never_sells_core_shares_and_rounds_down()
    {
        var parameters = CreateParameters();

        var result = TradeQuantityCalculator.Calculate(
            parameters,
            "available",
            "passed",
            "partial_trim",
            10m,
            1000,
            400,
            0,
            0m,
            null,
            0m);

        Assert.Equal(0, result.SuggestedBuyShares);
        Assert.Equal(100, result.SuggestedSellShares);
    }

    [Fact]
    public void Calculate_returns_no_action_when_model_is_not_available()
    {
        var result = TradeQuantityCalculator.Calculate(
            CreateParameters(),
            "cautious",
            "cautious",
            "strong_buy",
            10m,
            0,
            0,
            0,
            5000m,
            10000m,
            0m);

        Assert.Equal(0, result.SuggestedBuyShares);
        Assert.Equal(0, result.SuggestedSellShares);
        Assert.Equal(0m, result.SuggestedTradeAmount);
    }

    private static ModelParameterSet CreateParameters()
        => ModelParameterSet.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "v1",
            0.08m,
            0.06m,
            0.04m,
            0.03m,
            0.5m,
            0.25m,
            0.25m,
            0.5m,
            0.5m,
            0.8m,
            0.2m,
            3000m,
            5000m,
            0.001m,
            1m,
            100,
            new DateOnly(2026, 1, 1));
}
