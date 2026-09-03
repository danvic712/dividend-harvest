using DividendHarvest.Domain.Models;

namespace DividendHarvest.Domain.Portfolio;

public static class PortfolioTradeCashLedger
{
    public static IReadOnlyList<CashLedgerEntry> CreateEntries(PortfolioTrade trade)
    {
        ArgumentNullException.ThrowIfNull(trade);

        var principalAmount = trade.ShareQuantity * trade.PricePerShare;
        var entries = new List<CashLedgerEntry>
        {
            CashLedgerEntry.Create(
                trade.PortfolioId,
                trade.SecurityId,
                trade.TradeDate,
                trade.TradeDirectionCode,
                trade.TradeDirectionCode == TradeDirectionCodes.Buy
                    ? CashLedgerCodes.Outflow
                    : CashLedgerCodes.Inflow,
                principalAmount,
                $"portfolio_trade:{trade.Id}:principal")
        };

        if (trade.TransactionFeeAmount > 0)
        {
            entries.Add(
                CashLedgerEntry.Create(
                    trade.PortfolioId,
                    trade.SecurityId,
                    trade.TradeDate,
                    CashLedgerCodes.Fee,
                    CashLedgerCodes.Outflow,
                    trade.TransactionFeeAmount,
                    $"portfolio_trade:{trade.Id}:fee"));
        }

        return entries;
    }
}
