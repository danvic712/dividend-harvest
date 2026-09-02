using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class GetStockModelParametersRequestValidator
    : AbstractValidator<GetStockModelParametersRequest>
{
    public GetStockModelParametersRequestValidator()
    {
        AShareValidationRules.Add(this, x => x.SecurityCode, x => x.ExchangeCode);
    }
}
