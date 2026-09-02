using DividendHarvest.Domain.Models;

namespace DividendHarvest.Domain.Portfolio;

public static class PortfolioBudgetCalculator
{
    public static decimal CalculateAvailableBudget(
        decimal cashBalanceAmount,
        decimal portfolioMarketValue,
        decimal cashReserveRatio)
    {
        if (portfolioMarketValue < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(portfolioMarketValue),
                portfolioMarketValue,
                "组合市值不能为负数。");
        }

        if (cashReserveRatio is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cashReserveRatio),
                cashReserveRatio,
                "现金保留比例必须介于 0 和 1 之间。");
        }

        return Math.Max(
            cashBalanceAmount - portfolioMarketValue * cashReserveRatio,
            0m);
    }

    public static decimal CalculateCashBalance(
        IEnumerable<CashLedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries
            .Sum(entry => entry.CashDirectionCode == "inflow"
                ? entry.CashAmount
                : -entry.CashAmount);
    }

    public static decimal CalculateCurrentCashReserveRatio(
        IEnumerable<ModelParameterSet> parameters,
        DateOnly currentDate)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (currentDate == DateOnly.MinValue)
        {
            throw new ArgumentException("参数日期不能为空。", nameof(currentDate));
        }

        return parameters
            .Where(parameter => parameter.EffectiveFromDate <= currentDate)
            .GroupBy(parameter => parameter.SecurityId)
            .Select(group => group
                .OrderByDescending(parameter => parameter.EffectiveFromDate)
                .First()
                .CashReserveRatio)
            .DefaultIfEmpty(0m)
            .Max();
    }

    public static bool HasCompleteMarketValue(
        IEnumerable<PortfolioPosition> positions,
        IReadOnlySet<Guid> pricedSecurityIds)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(pricedSecurityIds);

        return positions
            .Where(position => position.HeldShares > 0)
            .All(position => pricedSecurityIds.Contains(position.SecurityId));
    }
}
