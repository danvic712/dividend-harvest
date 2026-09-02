namespace DividendHarvest.Domain.Models;

public sealed class ModelParameterSet
{
    private ModelParameterSet()
    {
    }

    public Guid Id { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Guid SecurityId { get; private set; }

    public string ModelVersion { get; private set; } = string.Empty;

    public decimal StrongBuyYieldThreshold { get; private set; }

    public decimal AccumulationYieldThreshold { get; private set; }

    public decimal PartialTrimYieldThreshold { get; private set; }

    public decimal AggressiveTrimYieldThreshold { get; private set; }

    public decimal StrongBuyBudgetRatio { get; private set; }

    public decimal AccumulateBudgetRatio { get; private set; }

    public decimal PartialTrimRatio { get; private set; }

    public decimal AggressiveTrimRatio { get; private set; }

    public decimal MaxSecurityWeight { get; private set; }

    public decimal MaxSectorWeight { get; private set; }

    public decimal CashReserveRatio { get; private set; }

    public decimal MaxSingleTradeAmount { get; private set; }

    public decimal MaxPeriodBudgetAmount { get; private set; }

    public decimal TransactionFeeRatio { get; private set; }

    public decimal MinimumTransactionFeeAmount { get; private set; }

    public int TradingLotSize { get; private set; }

    public DateOnly EffectiveFromDate { get; private set; }

    public static ModelParameterSet Create(
        Guid portfolioId,
        Guid securityId,
        string modelVersion,
        decimal strongBuyYieldThreshold,
        decimal accumulationYieldThreshold,
        decimal partialTrimYieldThreshold,
        decimal aggressiveTrimYieldThreshold,
        decimal strongBuyBudgetRatio,
        decimal accumulateBudgetRatio,
        decimal partialTrimRatio,
        decimal aggressiveTrimRatio,
        decimal maxSecurityWeight,
        decimal maxSectorWeight,
        decimal cashReserveRatio,
        decimal maxSingleTradeAmount,
        decimal maxPeriodBudgetAmount,
        decimal transactionFeeRatio,
        decimal minimumTransactionFeeAmount,
        int tradingLotSize,
        DateOnly effectiveFromDate)
    {
        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException("投资组合标识不能为空。", nameof(portfolioId));
        }

        if (securityId == Guid.Empty)
        {
            throw new ArgumentException("股票标识不能为空。", nameof(securityId));
        }

        if (string.IsNullOrWhiteSpace(modelVersion))
        {
            throw new ArgumentException("模型版本不能为空。", nameof(modelVersion));
        }

        EnsurePositiveRatio(strongBuyYieldThreshold, nameof(strongBuyYieldThreshold));
        EnsurePositiveRatio(accumulationYieldThreshold, nameof(accumulationYieldThreshold));
        EnsurePositiveRatio(partialTrimYieldThreshold, nameof(partialTrimYieldThreshold));
        EnsurePositiveRatio(aggressiveTrimYieldThreshold, nameof(aggressiveTrimYieldThreshold));

        if (strongBuyYieldThreshold <= accumulationYieldThreshold)
        {
            throw new ArgumentException(
                "强买入收益率阈值必须高于分批加仓收益率阈值。",
                nameof(strongBuyYieldThreshold));
        }

        if (accumulationYieldThreshold <= partialTrimYieldThreshold)
        {
            throw new ArgumentException(
                "分批加仓收益率阈值必须高于减仓候选收益率阈值。",
                nameof(partialTrimYieldThreshold));
        }

        if (partialTrimYieldThreshold <= aggressiveTrimYieldThreshold)
        {
            throw new ArgumentException(
                "减仓候选收益率阈值必须高于激进减仓收益率阈值。",
                nameof(aggressiveTrimYieldThreshold));
        }

        EnsureRatio(strongBuyBudgetRatio, nameof(strongBuyBudgetRatio));
        EnsureRatio(accumulateBudgetRatio, nameof(accumulateBudgetRatio));
        EnsureRatio(partialTrimRatio, nameof(partialTrimRatio));
        EnsureRatio(aggressiveTrimRatio, nameof(aggressiveTrimRatio));
        EnsureRatio(maxSecurityWeight, nameof(maxSecurityWeight));
        EnsureRatio(maxSectorWeight, nameof(maxSectorWeight));
        EnsureRatio(cashReserveRatio, nameof(cashReserveRatio));
        EnsureNonNegative(maxSingleTradeAmount, nameof(maxSingleTradeAmount));
        EnsureNonNegative(maxPeriodBudgetAmount, nameof(maxPeriodBudgetAmount));
        EnsureRatio(transactionFeeRatio, nameof(transactionFeeRatio));
        EnsureNonNegative(minimumTransactionFeeAmount, nameof(minimumTransactionFeeAmount));

        if (tradingLotSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tradingLotSize),
                tradingLotSize,
                "交易单位必须大于零。");
        }

        if (effectiveFromDate == DateOnly.MinValue)
        {
            throw new ArgumentException("参数生效日期不能为空。", nameof(effectiveFromDate));
        }

        return new ModelParameterSet
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            SecurityId = securityId,
            ModelVersion = modelVersion.Trim(),
            StrongBuyYieldThreshold = strongBuyYieldThreshold,
            AccumulationYieldThreshold = accumulationYieldThreshold,
            PartialTrimYieldThreshold = partialTrimYieldThreshold,
            AggressiveTrimYieldThreshold = aggressiveTrimYieldThreshold,
            StrongBuyBudgetRatio = strongBuyBudgetRatio,
            AccumulateBudgetRatio = accumulateBudgetRatio,
            PartialTrimRatio = partialTrimRatio,
            AggressiveTrimRatio = aggressiveTrimRatio,
            MaxSecurityWeight = maxSecurityWeight,
            MaxSectorWeight = maxSectorWeight,
            CashReserveRatio = cashReserveRatio,
            MaxSingleTradeAmount = maxSingleTradeAmount,
            MaxPeriodBudgetAmount = maxPeriodBudgetAmount,
            TransactionFeeRatio = transactionFeeRatio,
            MinimumTransactionFeeAmount = minimumTransactionFeeAmount,
            TradingLotSize = tradingLotSize,
            EffectiveFromDate = effectiveFromDate
        };
    }

    private static void EnsurePositiveRatio(decimal value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "收益率阈值必须大于零。");
        }
    }

    private static void EnsureRatio(decimal value, string parameterName)
    {
        if (value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "比例必须介于 0 和 1 之间。");
        }
    }

    private static void EnsureNonNegative(decimal value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "金额不能为负数。");
        }
    }
}
