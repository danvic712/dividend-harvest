using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Securities;
using FluentValidation;

namespace DividendHarvest.Application.Stocks;

public sealed class StockPriceObservationAppService(
    IStockFactSyncAppService stockFactSyncAppService,
    IValidator<SyncStockPriceRequest> requestValidator)
    : IStockPriceObservationAppService
{
    public async Task<StockPriceObservationResult> SyncAsync(
        SyncStockPriceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationResult = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw ApplicationErrors.Validation(
                ApplicationErrorCodes.StockDataSyncValidationFailed,
                ValidationErrorFormatter.Format(validationResult));
        }

        var reference = AShareReference.Create(request.SecurityCode, request.ExchangeCode);
        return await stockFactSyncAppService.SyncPriceAsync(
            reference,
            cancellationToken);
    }
}
