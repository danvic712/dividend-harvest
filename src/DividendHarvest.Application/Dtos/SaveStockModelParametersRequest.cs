namespace DividendHarvest.Application.Dtos;

public sealed record SaveStockModelParametersRequest(
    string SecurityCode,
    string ExchangeCode,
    string ModelVersion,
    decimal StrongBuyYieldThreshold,
    decimal AccumulationYieldThreshold,
    decimal PartialTrimYieldThreshold,
    decimal AggressiveTrimYieldThreshold,
    decimal StrongBuyBudgetRatio,
    decimal AccumulateBudgetRatio,
    decimal PartialTrimRatio,
    decimal AggressiveTrimRatio,
    decimal MaxSecurityWeight,
    decimal MaxSectorWeight,
    decimal CashReserveRatio,
    decimal MaxSingleTradeAmount,
    decimal MaxPeriodBudgetAmount,
    decimal TransactionFeeRatio,
    decimal MinimumTransactionFeeAmount,
    int TradingLotSize,
    DateOnly EffectiveFromDate);
