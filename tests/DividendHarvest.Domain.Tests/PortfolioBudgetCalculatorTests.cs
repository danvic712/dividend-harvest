using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Portfolio;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class PortfolioBudgetCalculatorTests
{
    [Fact]
    public void CalculateAvailableBudget_subtracts_the_portfolio_cash_reserve()
    {
        var result = PortfolioBudgetCalculator.CalculateAvailableBudget(5000m, 10000m, 0.1m);

        Assert.Equal(4000m, result);
    }

    [Fact]
    public void CalculateCashBalance_returns_a_signed_ledger_balance()
    {
        var portfolioId = Guid.NewGuid();
        var entries = new[]
        {
            CashLedgerEntry.Create(
                portfolioId,
                null,
                new DateOnly(2026, 9, 1),
                "budget_deposit",
                "inflow",
                5000m,
                "deposit-1"),
            CashLedgerEntry.Create(
                portfolioId,
                null,
                new DateOnly(2026, 9, 2),
                "fee",
                "outflow",
                10m,
                "fee-1")
        };

        Assert.Equal(4990m, PortfolioBudgetCalculator.CalculateCashBalance(entries));
    }

    [Fact]
    public void CalculateCurrentCashReserveRatio_uses_the_latest_parameter_per_security()
    {
        var portfolioId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var parameters = new[]
        {
            CreateParameters(portfolioId, securityId, 0.9m, new DateOnly(2025, 1, 1)),
            CreateParameters(portfolioId, securityId, 0.1m, new DateOnly(2026, 1, 1)),
            CreateParameters(portfolioId, Guid.NewGuid(), 0.3m, new DateOnly(2026, 1, 1))
        };

        var result = PortfolioBudgetCalculator.CalculateCurrentCashReserveRatio(
            parameters,
            new DateOnly(2026, 9, 1));

        Assert.Equal(0.3m, result);
    }

    [Fact]
    public void HasCompleteMarketValue_requires_a_price_for_every_held_security()
    {
        var pricedSecurityId = Guid.NewGuid();
        var unpricedSecurityId = Guid.NewGuid();
        var positions = new[]
        {
            new PortfolioPosition
            {
                SecurityId = pricedSecurityId,
                HeldShares = 100
            },
            new PortfolioPosition
            {
                SecurityId = unpricedSecurityId,
                HeldShares = 50
            }
        };

        Assert.False(
            PortfolioBudgetCalculator.HasCompleteMarketValue(
                positions,
                new HashSet<Guid> { pricedSecurityId }));
        Assert.True(
            PortfolioBudgetCalculator.HasCompleteMarketValue(
                positions,
                new HashSet<Guid> { pricedSecurityId, unpricedSecurityId }));
    }

    private static ModelParameterSet CreateParameters(
        Guid portfolioId,
        Guid securityId,
        decimal cashReserveRatio,
        DateOnly effectiveFromDate)
        => ModelParameterSet.Create(
            portfolioId,
            securityId,
            "v1",
            0.08m,
            0.06m,
            0.04m,
            0.03m,
            0.5m,
            0.25m,
            0.25m,
            0.5m,
            0.2m,
            0.4m,
            cashReserveRatio,
            1000m,
            5000m,
            0.001m,
            5m,
            100,
            effectiveFromDate);
}
