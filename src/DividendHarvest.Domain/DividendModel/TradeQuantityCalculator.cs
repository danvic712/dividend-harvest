using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Codes;

namespace DividendHarvest.Domain.DividendModel;

public static class TradeQuantityCalculator
{
    public static TradeQuantityResult Calculate(
        ModelParameterSet parameters,
        string modelStatusCode,
        string dividendReliabilityCode,
        string priceZoneCode,
        decimal closePrice,
        int heldShares,
        int coreShares,
        int targetShares,
        decimal availableBudgetAmount,
        decimal? totalPortfolioValue,
        decimal currentSecurityMarketValue,
        decimal? currentSectorMarketValue = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (closePrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(closePrice),
                closePrice,
                "当前价格必须大于零。");
        }

        if (heldShares < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heldShares),
                heldShares,
                "当前持股数量不能为负数。");
        }

        if (coreShares < 0 || coreShares > heldShares)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coreShares),
                coreShares,
                "核心仓数量不能为负数或超过当前持股数量。");
        }

        if (targetShares < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetShares),
                targetShares,
                "目标股数不能为负数。");
        }

        if (availableBudgetAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableBudgetAmount),
                availableBudgetAmount,
                "可用预算不能为负数。");
        }

        if (totalPortfolioValue is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalPortfolioValue),
                totalPortfolioValue,
                "组合总市值不能为负数。");
        }

        if (currentSecurityMarketValue < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentSecurityMarketValue),
                currentSecurityMarketValue,
                "单只股票当前市值不能为负数。");
        }

        var normalizedModelStatusCode = NormalizeRequiredCode(modelStatusCode, ModelStatusCodes.IsSupported, nameof(modelStatusCode));
        var normalizedReliabilityCode = NormalizeRequiredCode(
            dividendReliabilityCode,
            DividendReliabilityCodes.IsSupported,
            nameof(dividendReliabilityCode));
        var normalizedPriceZoneCode = NormalizeRequiredCode(
            priceZoneCode,
            PriceZoneCodes.IsSupported,
            nameof(priceZoneCode));

        if (normalizedModelStatusCode != ModelStatusCodes.Available
            || normalizedReliabilityCode != DividendReliabilityCodes.Passed)
        {
            return EmptyResult();
        }

        return normalizedPriceZoneCode switch
        {
            PriceZoneCodes.StrongBuy or PriceZoneCodes.Accumulate => CalculateBuy(
                parameters,
                normalizedPriceZoneCode,
                closePrice,
                targetShares,
                heldShares,
                availableBudgetAmount,
                totalPortfolioValue,
                currentSecurityMarketValue,
                currentSectorMarketValue),
            PriceZoneCodes.PartialTrim or PriceZoneCodes.AggressiveTrim => CalculateSell(
                parameters,
                normalizedPriceZoneCode,
                closePrice,
                heldShares,
                coreShares),
            _ => EmptyResult()
        };
    }

    private static TradeQuantityResult CalculateBuy(
        ModelParameterSet parameters,
        string priceZoneCode,
        decimal closePrice,
        int targetShares,
        int heldShares,
        decimal availableBudgetAmount,
        decimal? totalPortfolioValue,
        decimal currentSecurityMarketValue,
        decimal? currentSectorMarketValue)
    {
        var budgetRatio = priceZoneCode == PriceZoneCodes.StrongBuy
            ? parameters.StrongBuyBudgetRatio
            : parameters.AccumulateBudgetRatio;
        var maximumBudget = availableBudgetAmount * budgetRatio;

        if (parameters.MaxPeriodBudgetAmount > 0)
        {
            maximumBudget = Math.Min(maximumBudget, parameters.MaxPeriodBudgetAmount);
        }

        if (parameters.MaxSingleTradeAmount > 0)
        {
            maximumBudget = Math.Min(maximumBudget, parameters.MaxSingleTradeAmount);
        }

        if (targetShares > 0)
        {
            var targetRoom = Math.Max(targetShares - heldShares, 0) * closePrice;
            maximumBudget = Math.Min(maximumBudget, targetRoom);
        }

        if (totalPortfolioValue is { } portfolioValue)
        {
            var securityRoom = Math.Max(
                portfolioValue * parameters.MaxSecurityWeight
                    - currentSecurityMarketValue,
                0m);
            maximumBudget = Math.Min(maximumBudget, securityRoom);

            if (currentSectorMarketValue is { } sectorValue)
            {
                var sectorRoom = Math.Max(
                    portfolioValue * parameters.MaxSectorWeight - sectorValue,
                    0m);
                maximumBudget = Math.Min(maximumBudget, sectorRoom);
            }
        }

        var shares = FloorToTradingLot(
            maximumBudget / closePrice,
            parameters.TradingLotSize);
        while (shares > 0)
        {
            var tradeAmount = shares * closePrice;
            var fee = CalculateFee(parameters, tradeAmount);
            if (tradeAmount + fee <= availableBudgetAmount)
            {
                return new TradeQuantityResult(shares, 0, tradeAmount, fee);
            }

            shares -= parameters.TradingLotSize;
        }

        return EmptyResult();
    }

    private static TradeQuantityResult CalculateSell(
        ModelParameterSet parameters,
        string priceZoneCode,
        decimal closePrice,
        int heldShares,
        int coreShares)
    {
        var satelliteShares = Math.Max(heldShares - coreShares, 0);
        var sellRatio = priceZoneCode == PriceZoneCodes.PartialTrim
            ? parameters.PartialTrimRatio
            : parameters.AggressiveTrimRatio;
        var shares = FloorToTradingLot(
            satelliteShares * sellRatio,
            parameters.TradingLotSize);
        var tradeAmount = shares * closePrice;

        return new TradeQuantityResult(
            0,
            shares,
            tradeAmount,
            CalculateFee(parameters, tradeAmount));
    }

    private static int FloorToTradingLot(decimal shares, int tradingLotSize)
    {
        if (shares <= 0)
        {
            return 0;
        }

        var lotCount = decimal.Floor(shares / tradingLotSize);
        return lotCount > int.MaxValue / tradingLotSize
            ? int.MaxValue / tradingLotSize * tradingLotSize
            : decimal.ToInt32(lotCount) * tradingLotSize;
    }

    private static decimal CalculateFee(
        ModelParameterSet parameters,
        decimal tradeAmount)
        => tradeAmount <= 0
            ? 0m
            : Math.Max(
                tradeAmount * parameters.TransactionFeeRatio,
                parameters.MinimumTransactionFeeAmount);

    private static TradeQuantityResult EmptyResult() => new(0, 0, 0m, 0m);

    private static string NormalizeRequiredCode(
        string value,
        Func<string?, bool> isSupported,
        string parameterName)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!isSupported(normalized))
        {
            throw new ArgumentException("业务代码不受支持。", parameterName);
        }

        return normalized;
    }
}
