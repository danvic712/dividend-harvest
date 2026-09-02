using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Portfolio;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class RecordPortfolioTradeRequestValidator
    : AbstractValidator<RecordPortfolioTradeRequest>
{
    public RecordPortfolioTradeRequestValidator()
    {
        AShareValidationRules.Add(this, x => x.SecurityCode, x => x.ExchangeCode);

        RuleFor(x => x.TradeDate)
            .NotEqual(DateOnly.MinValue)
            .WithMessage("交易日期不能为空。");
        RuleFor(x => x.TradeDirectionCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(TradeDirectionCodes.IsSupported)
            .WithMessage("交易方向必须是 buy 或 sell。");
        RuleFor(x => x.ShareQuantity)
            .GreaterThan(0)
            .WithMessage("交易股数必须大于零。");
        RuleFor(x => x.PricePerShare)
            .GreaterThan(0)
            .WithMessage("成交价格必须大于零。");
        RuleFor(x => x.TransactionFeeAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("交易费用不能为负数。");
        RuleFor(x => x.SourceRecordId)
            .MaximumLength(200)
            .WithMessage("来源记录标识不能超过 200 个字符。");
    }
}
