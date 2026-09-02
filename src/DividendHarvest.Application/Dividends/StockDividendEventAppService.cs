using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Dividends;

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

        IReadOnlyList<StockDividendData>? dividendData;
        try
        {
            dividendData = await stockDataProvider.GetDividendEventsAsync(
                reference,
                cancellationToken);
        }
        catch (StockDataProviderUnavailableException exception)
        {
            throw new StockDividendDataUnavailableException(
                reference.SecurityCode,
                exception);
        }

        if (dividendData is null)
        {
            throw new StockDividendDataUnavailableException(reference.SecurityCode);
        }

        var eventRepository = uow.Get<DividendEvent>();
        var existingEvents = await eventRepository
            .GetQueryable(asNoTracking: true)
            .Where(dividendEvent => dividendEvent.SecurityId == security.Id)
            .ToListAsync(cancellationToken);
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
                throw new StockDividendDataUnavailableException(reference.SecurityCode);
            }

            if (!seenSourceRecordIds.Add(data.SourceRecordId))
            {
                throw new StockDividendDataUnavailableException(reference.SecurityCode);
            }

            if (existingBySourceRecordId.TryGetValue(
                    data.SourceRecordId,
                    out var existingEvent))
            {
                results.Add(ToResult(existingEvent, reference));
                continue;
            }

            var dividendEvent = CreateDividendEvent(security.Id, data);
            newEvents.Add(dividendEvent);
            results.Add(ToResult(dividendEvent, reference));
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
            throw new StockDividendDataUnavailableException(
                data.SecurityCode,
                exception);
        }
    }

    private static StockDividendEventResult ToResult(
        DividendEvent dividendEvent,
        AShareReference reference)
        => new(
            dividendEvent.Id,
            reference.SecurityCode,
            reference.ExchangeCode,
            dividendEvent.DividendPerShare,
            dividendEvent.DividendTypeCode,
            dividendEvent.DividendStatusCode,
            dividendEvent.AnnouncementDate,
            dividendEvent.ExDividendDate,
            dividendEvent.PaymentDate,
            dividendEvent.IsSpecialDividend,
            dividendEvent.PublishedAt,
            dividendEvent.CapturedAt,
            dividendEvent.DataSource,
            dividendEvent.SourceRecordId,
            dividendEvent.DataQualityCode);
}
