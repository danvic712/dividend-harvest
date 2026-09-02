using System.Linq.Expressions;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

internal static class AShareValidationRules
{
    private static readonly string[] SupportedExchanges = ["SSE", "SZSE", "BSE"];

    public static void Add<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string?>> securityCode,
        Expression<Func<T, string?>> exchangeCode)
    {
        validator.RuleFor(securityCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(IsValidSecurityCode)
            .WithMessage("A 股股票代码必须是 6 位数字。");

        validator.RuleFor(exchangeCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(IsSupportedExchange)
            .WithMessage("交易所必须是 SSE、SZSE 或 BSE。");
    }

    private static bool IsValidSecurityCode(string? value)
        => value?.Trim().Length == 6
            && value.Trim().All(character => character is >= '0' and <= '9');

    private static bool IsSupportedExchange(string? value)
        => value is not null
            && SupportedExchanges.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}
