using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class SyncStockPriceRequestValidator
    : AbstractValidator<SyncStockPriceRequest>
{
    public SyncStockPriceRequestValidator()
    {
        AShareValidationRules.Add(this, x => x.SecurityCode, x => x.ExchangeCode);
    }
}
