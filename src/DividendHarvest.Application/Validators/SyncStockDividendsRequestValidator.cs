using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class SyncStockDividendsRequestValidator
    : AbstractValidator<SyncStockDividendsRequest>
{
    public SyncStockDividendsRequestValidator()
    {
        AShareValidationRules.Add(this, x => x.SecurityCode, x => x.ExchangeCode);
    }
}
