using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class SyncStockFinancialsRequestValidator
    : AbstractValidator<SyncStockFinancialsRequest>
{
    public SyncStockFinancialsRequestValidator()
    {
        AShareValidationRules.Add(this, x => x.SecurityCode, x => x.ExchangeCode);
    }
}
