using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Stocks;

public sealed class StockPriceObservationAppService(
    IUow uow,
    IStockDataProvider stockDataProvider,
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
            throw new StockDataSyncValidationException(
                ValidationErrorFormatter.Format(validationResult));
        }

        var reference = AShareReference.Create(request.SecurityCode, request.ExchangeCode);
        var security = await uow.Get<Security>()
            .GetQueryable(asNoTracking: true)
            .SingleOrDefaultAsync(
                item =>
                    item.SecurityCode == reference.SecurityCode
                    && item.ExchangeCode == reference.ExchangeCode,
                cancellationToken);
        if (security is null)
        {
            throw new StockNotConfiguredException(
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        StockMarketData? marketData;
        try
        {
            marketData = await stockDataProvider.GetMarketDataAsync(
                reference,
                cancellationToken);
        }
        catch (StockDataProviderUnavailableException exception)
        {
            throw new StockMarketDataUnavailableException(
                reference.SecurityCode,
                exception);
        }

        if (marketData is null || !MatchesReference(reference, marketData))
        {
            throw new StockMarketDataUnavailableException(reference.SecurityCode);
        }

        PriceObservation? existingObservation = await uow.Get<PriceObservation>()
            .GetQueryable(asNoTracking: true)
            .SingleOrDefaultAsync(
                observation =>
                    observation.SecurityId == security.Id
                    && observation.TradingDate == marketData.TradingDate,
                cancellationToken);
        if (existingObservation is not null)
        {
            return ToResult(existingObservation, reference);
        }

        var observation = CreateObservation(security.Id, marketData);
        await uow.Get<PriceObservation>().AddAsync(observation, cancellationToken);
        await uow.CommitAsync(cancellationToken);

        return ToResult(observation, reference);
    }

    private static bool MatchesReference(
        AShareReference reference,
        StockMarketData marketData)
    {
        try
        {
            return AShareReference.Create(
                marketData.SecurityCode,
                marketData.ExchangeCode) == reference;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static PriceObservation CreateObservation(
        Guid securityId,
        StockMarketData marketData)
    {
        try
        {
            return PriceObservation.Create(
                securityId,
                marketData.TradingDate,
                marketData.ClosePrice,
                marketData.PriceObservedAt,
                marketData.DataSource,
                marketData.SourceRecordId,
                marketData.DataQualityCode);
        }
        catch (ArgumentException exception)
        {
            throw new StockMarketDataUnavailableException(
                marketData.SecurityCode,
                exception);
        }
    }

    private static StockPriceObservationResult ToResult(
        PriceObservation observation,
        AShareReference reference)
        => new(
            observation.Id,
            reference.SecurityCode,
            reference.ExchangeCode,
            observation.TradingDate,
            observation.ClosePrice,
            observation.PriceObservedAt,
            observation.DataSource,
            observation.SourceRecordId,
            observation.DataQualityCode);
}
