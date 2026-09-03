using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Mapping;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using FluentValidation;

namespace DividendHarvest.Application.Stocks;

public sealed class StockDividendEventAppService(
    IUow uow,
    IStockDataProvider stockDataProvider,
    IValidator<SyncStockDividendsRequest> requestValidator)
    : IStockDividendEventAppService
{
    public async Task<IReadOnlyList<StockDividendEventResult>> SyncAsync(
        SyncStockDividendsRequest request,
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
        var security = await uow.Get<Security>()
            .SingleOrDefaultAsync(
                item =>
                    item.SecurityCode == reference.SecurityCode
                    && item.ExchangeCode == reference.ExchangeCode,
                cancellationToken);
        if (security is null)
        {
            throw ApplicationErrors.WithSecurityReference(
                ApplicationErrorCodes.StockNotConfigured,
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        IReadOnlyList<StockDividendData>? dividendData;
        try
        {
            dividendData = await stockDataProvider.GetDividendEventsAsync(
                reference,
                cancellationToken);
        }
        catch (Exception exception) when (exception is IStockDataProviderFailure)
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockDividendDataUnavailable,
                reference.SecurityCode,
                exception);
        }

        if (dividendData is null)
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockDividendDataUnavailable,
                reference.SecurityCode);
        }

        var eventRepository = uow.Get<DividendEvent>();
        var existingEvents = await eventRepository
            .ListAsync(
                dividendEvent => dividendEvent.SecurityId == security.Id,
                cancellationToken: cancellationToken);
        var existingBySourceRecordId = existingEvents.ToDictionary(
            dividendEvent => dividendEvent.SourceRecordId,
            StringComparer.Ordinal);
        var seenSourceRecordIds = new HashSet<string>(StringComparer.Ordinal);
        var newEvents = new List<DividendEvent>();
        var results = new List<StockDividendEventResult>(dividendData.Count);

        foreach (var data in dividendData)
        {
            if (data is null || !MatchesReference(reference, data))
            {
                throw ApplicationErrors.WithSecurity(
                    ApplicationErrorCodes.StockDividendDataUnavailable,
                    reference.SecurityCode);
            }

            if (!seenSourceRecordIds.Add(data.SourceRecordId))
            {
                throw ApplicationErrors.WithSecurity(
                    ApplicationErrorCodes.StockDividendDataUnavailable,
                    reference.SecurityCode);
            }

            if (existingBySourceRecordId.TryGetValue(
                    data.SourceRecordId,
                    out var existingEvent))
            {
                results.Add(ApplicationMapper.ToStockDividendEventResult(
                    existingEvent,
                    reference.SecurityCode,
                    reference.ExchangeCode));
                continue;
            }

            var dividendEvent = CreateDividendEvent(security.Id, data);
            newEvents.Add(dividendEvent);
            results.Add(ApplicationMapper.ToStockDividendEventResult(
                dividendEvent,
                reference.SecurityCode,
                reference.ExchangeCode));
            existingBySourceRecordId.Add(data.SourceRecordId, dividendEvent);
        }

        foreach (var dividendEvent in newEvents)
        {
            await eventRepository.AddAsync(dividendEvent, cancellationToken);
        }

        if (newEvents.Count > 0)
        {
            await uow.CommitAsync(cancellationToken);
        }

        return results;
    }

    private static bool MatchesReference(
        AShareReference reference,
        StockDividendData data)
    {
        try
        {
            return AShareReference.Create(data.SecurityCode, data.ExchangeCode) == reference;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static DividendEvent CreateDividendEvent(
        Guid securityId,
        StockDividendData data)
    {
        try
        {
            return DividendEvent.Create(
                securityId,
                data.DividendPerShare,
                data.DividendTypeCode,
                data.DividendStatusCode,
                data.AnnouncementDate,
                data.ExDividendDate,
                data.PaymentDate,
                data.IsSpecialDividend,
                data.PublishedAt,
                data.CapturedAt,
                data.DataSource,
                data.SourceRecordId,
                data.DataQualityCode);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockDividendDataUnavailable,
                data.SecurityCode,
                exception);
        }
    }

}
