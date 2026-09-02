using System.Linq.Expressions;
using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class SaveStockModelParametersRequestValidator
    : AbstractValidator<SaveStockModelParametersRequest>
{
    public SaveStockModelParametersRequestValidator()
    {
        AShareValidationRules.Add(this, x => x.SecurityCode, x => x.ExchangeCode);

        RuleFor(x => x.ModelVersion)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("模型版本不能为空。")
            .MaximumLength(32)
            .WithMessage("模型版本不能超过 32 个字符。");

        RuleFor(x => x.StrongBuyYieldThreshold)
            .GreaterThan(0)
            .WithMessage("收益率阈值必须大于零。");
        RuleFor(x => x.AccumulationYieldThreshold)
            .GreaterThan(0)
            .WithMessage("收益率阈值必须大于零。");
        RuleFor(x => x.PartialTrimYieldThreshold)
            .GreaterThan(0)
            .WithMessage("收益率阈值必须大于零。");
        RuleFor(x => x.AggressiveTrimYieldThreshold)
            .GreaterThan(0)
            .WithMessage("收益率阈值必须大于零。");

        RuleFor(x => x.AccumulationYieldThreshold)
            .Must((request, value) => request.StrongBuyYieldThreshold > value)
            .WithMessage("强买入收益率阈值必须高于分批加仓收益率阈值。");
        RuleFor(x => x.PartialTrimYieldThreshold)
            .Must((request, value) => request.AccumulationYieldThreshold > value)
            .WithMessage("分批加仓收益率阈值必须高于减仓候选收益率阈值。");
        RuleFor(x => x.AggressiveTrimYieldThreshold)
            .Must((request, value) => request.PartialTrimYieldThreshold > value)
            .WithMessage("减仓候选收益率阈值必须高于激进减仓收益率阈值。");

        AddRatioRule(x => x.StrongBuyBudgetRatio, "强买入预算比例");
        AddRatioRule(x => x.AccumulateBudgetRatio, "分批加仓预算比例");
        AddRatioRule(x => x.PartialTrimRatio, "减仓候选比例");
        AddRatioRule(x => x.AggressiveTrimRatio, "激进减仓比例");
        AddRatioRule(x => x.MaxSecurityWeight, "单只股票最大权重");
        AddRatioRule(x => x.MaxSectorWeight, "单一行业最大权重");
        AddRatioRule(x => x.CashReserveRatio, "现金保留比例");
        AddRatioRule(x => x.TransactionFeeRatio, "交易费用比例");

        RuleFor(x => x.MaxSingleTradeAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("单次交易金额上限不能为负数。");
        RuleFor(x => x.MaxPeriodBudgetAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("单期预算上限不能为负数。");
        RuleFor(x => x.MinimumTransactionFeeAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("最低交易费用不能为负数。");
        RuleFor(x => x.TradingLotSize)
            .GreaterThan(0)
            .WithMessage("交易单位必须大于零。");
        RuleFor(x => x.EffectiveFromDate)
            .NotEqual(DateOnly.MinValue)
            .WithMessage("参数生效日期不能为空。");
    }

    private void AddRatioRule(
        Expression<Func<SaveStockModelParametersRequest, decimal>> selector,
        string label)
    {
        RuleFor(selector)
            .InclusiveBetween(0, 1)
            .WithMessage($"{label}必须介于 0 和 1 之间。");
    }
}
