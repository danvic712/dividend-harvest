using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class SetupStockRequestValidator : AbstractValidator<SetupStockRequest>
{
    private static readonly string[] SupportedExchanges = ["SSE", "SZSE", "BSE"];

    public SetupStockRequestValidator(IValidator<InitialHoldingInput> initialHoldingValidator)
    {
        RuleFor(x => x.SecurityCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(IsValidSecurityCode)
            .WithMessage("A 股股票代码必须是 6 位数字。");

        RuleFor(x => x.ExchangeCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(IsSupportedExchange)
            .WithMessage("交易所必须是 SSE、SZSE 或 BSE。");

        When(x => x.InitialHolding is not null, () =>
        {
            RuleFor(x => x.InitialHolding!)
                .SetValidator(initialHoldingValidator);
        });
    }

    private static bool IsValidSecurityCode(string? value)
        => value?.Trim().Length == 6
            && value.Trim().All(character => character is >= '0' and <= '9');

    private static bool IsSupportedExchange(string? value)
        => value is not null
            && SupportedExchanges.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}
