using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Portfolio;
using Xunit;

namespace DividendHarvest.Domain.Tests;

public sealed class PortfolioTradeCashLedgerTests
{
    [Fact]
    public void CreateEntries_for_buy_trade_creates_principal_and_fee_outflows()
    {
        var portfolioId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var trade = PortfolioTrade.Create(
            portfolioId,
            securityId,
            new DateOnly(2026, 9, 1),
            TradeDirectionCodes.Buy,
            100,
            4m,
            5m,
            null);

        var entries = PortfolioTradeCashLedger.CreateEntries(trade);

        Assert.Collection(
            entries,
            principal =>
            {
                Assert.Equal(portfolioId, principal.PortfolioId);
                Assert.Equal(securityId, principal.SecurityId);
                Assert.Equal(CashLedgerCodes.Buy, principal.EntryTypeCode);
                Assert.Equal(CashLedgerCodes.Outflow, principal.CashDirectionCode);
                Assert.Equal(400m, principal.CashAmount);
                Assert.Equal($"portfolio_trade:{trade.Id}:principal", principal.SourceRecordId);
            },
            fee =>
            {
                Assert.Equal(CashLedgerCodes.Fee, fee.EntryTypeCode);
                Assert.Equal(CashLedgerCodes.Outflow, fee.CashDirectionCode);
                Assert.Equal(5m, fee.CashAmount);
                Assert.Equal($"portfolio_trade:{trade.Id}:fee", fee.SourceRecordId);
            });
    }

    [Fact]
    public void CreateEntries_for_sell_trade_creates_one_principal_inflow_without_fee()
    {
        var trade = PortfolioTrade.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            TradeDirectionCodes.Sell,
            50,
            6m,
            0m,
            null);

        var entry = Assert.Single(PortfolioTradeCashLedger.CreateEntries(trade));

        Assert.Equal(CashLedgerCodes.Sell, entry.EntryTypeCode);
        Assert.Equal(CashLedgerCodes.Inflow, entry.CashDirectionCode);
        Assert.Equal(300m, entry.CashAmount);
        Assert.Equal($"portfolio_trade:{trade.Id}:principal", entry.SourceRecordId);
    }
}
