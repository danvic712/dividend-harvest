using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Portfolio;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class RecordCashLedgerEntryRequestValidator
    : AbstractValidator<RecordCashLedgerEntryRequest>
{
    public RecordCashLedgerEntryRequestValidator()
    {
        RuleFor(x => x.EntryDate)
            .NotEqual(DateOnly.MinValue)
            .WithMessage("现金流水日期不能为空。");

        RuleFor(x => x.EntryTypeCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(CashLedgerCodes.IsSupportedEntryType)
            .WithMessage("现金流水类型不受支持。");

        RuleFor(x => x.CashDirectionCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(CashLedgerCodes.IsSupportedDirection)
            .WithMessage("现金流水方向不受支持。");

        RuleFor(x => x.CashDirectionCode)
            .Must((request, direction) =>
                CashLedgerCodes.IsCompatible(request.EntryTypeCode, direction))
            .WithMessage("现金流水类型和方向不匹配。");

        RuleFor(x => x.CashAmount)
            .GreaterThan(0)
            .WithMessage("现金流水金额必须大于零。");

        RuleFor(x => x.SourceRecordId)
            .MaximumLength(200)
            .WithMessage("来源记录标识不能超过 200 个字符。");

        RuleFor(x => x.SecurityCode)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || AShareValidationRules.IsValidSecurityCode(value))
            .WithMessage("A 股股票代码必须是 6 位数字。");

        RuleFor(x => x.ExchangeCode)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || AShareValidationRules.IsSupportedExchange(value))
            .WithMessage("交易所必须是 SSE、SZSE 或 BSE。");

        RuleFor(x => x.ExchangeCode)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.SecurityCode))
            .WithMessage("填写股票代码时必须同时填写交易所。");

        RuleFor(x => x.SecurityCode)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.ExchangeCode))
            .WithMessage("填写交易所时必须同时填写股票代码。");

        RuleFor(x => x.SecurityCode)
            .NotEmpty()
            .When(x => x.EntryTypeCode?.Trim().ToLowerInvariant() is
                "buy" or "sell" or "dividend_received")
            .WithMessage("买入、卖出和实际收到股息的流水必须关联股票。");

        RuleFor(x => x.ExchangeCode)
            .NotEmpty()
            .When(x => x.EntryTypeCode?.Trim().ToLowerInvariant() is
                "buy" or "sell" or "dividend_received")
            .WithMessage("买入、卖出和实际收到股息的流水必须关联交易所。");
    }

}
