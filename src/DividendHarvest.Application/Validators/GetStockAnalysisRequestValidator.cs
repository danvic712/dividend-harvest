using DividendHarvest.Application.Dtos;
using FluentValidation;

namespace DividendHarvest.Application.Validators;

public sealed class GetStockAnalysisRequestValidator
    : AbstractValidator<GetStockAnalysisRequest>
{
    public GetStockAnalysisRequestValidator()
    {
        AShareValidationRules.Add(this, x => x.SecurityCode, x => x.ExchangeCode);
    }
}
