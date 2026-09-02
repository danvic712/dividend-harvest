using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Securities;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class SetupRequestValidator : AbstractValidator<SetupRequest>
{
    public SetupRequestValidator(IValidator<SetupStockRequest> stockValidator)
    {
        RuleFor(x => x.PortfolioName)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("投资组合名称必须为 1 到 100 个字符。")
            .Must(value => value.Trim().Length <= 100)
            .WithMessage("投资组合名称必须为 1 到 100 个字符。");

        RuleFor(x => x.Stocks)
            .Must(stocks => stocks is { Count: > 0 })
            .WithMessage("至少需要配置一只 A 股股票。");

        RuleFor(x => x.Stocks)
            .Must(HaveUniqueStockReferences)
            .WithMessage("不能重复配置同一只股票。");

        RuleForEach(x => x.Stocks)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("股票配置不能为空。")
            .SetValidator(stockValidator);
    }

    private static bool HaveUniqueStockReferences(IReadOnlyList<SetupStockRequest>? stocks)
    {
        if (stocks is null)
        {
            return true;
        }

        var references = new HashSet<AShareReference>();
        foreach (var stock in stocks)
        {
            if (stock is null)
            {
                continue;
            }

            try
            {
                if (!references.Add(AShareReference.Create(stock.SecurityCode, stock.ExchangeCode)))
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // The child validator reports the malformed stock reference.
            }
        }

        return true;
    }
}
