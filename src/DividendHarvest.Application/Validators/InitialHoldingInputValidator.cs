using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class InitialHoldingInputValidator : AbstractValidator<InitialHoldingInput>
{
    public InitialHoldingInputValidator()
    {
        RuleFor(x => x.HeldShares)
            .GreaterThanOrEqualTo(0)
            .WithMessage("持股数量不能为负数。");

        RuleFor(x => x.CoreShares)
            .Must((input, coreShares) => coreShares >= 0 && coreShares <= input.HeldShares)
            .WithMessage("核心仓数量不能为负数或超过持股数量。");

        RuleFor(x => x.TargetShares)
            .GreaterThanOrEqualTo(0)
            .WithMessage("目标股数不能为负数。");

        RuleFor(x => x.AverageCostPerShare)
            .GreaterThanOrEqualTo(0)
            .WithMessage("平均成本不能为负数。");
    }
}
