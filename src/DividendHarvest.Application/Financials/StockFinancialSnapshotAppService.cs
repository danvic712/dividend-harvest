using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Financials;

public sealed class StockFinancialSnapshotAppService(
    IUow uow,
    IStockDataProvider stockDataProvider,
    IValidator<SyncStockFinancialsRequest> requestValidator)
    : IStockFinancialSnapshotAppService
{
    public async Task<IReadOnlyList<StockFinancialSnapshotResult>> SyncAsync(
        SyncStockFinancialsRequest request,
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

        IReadOnlyList<StockFinancialData>? financialData;
        try
        {
            financialData = await stockDataProvider.GetFinancialSnapshotsAsync(
                reference,
                cancellationToken);
        }
        catch (StockDataProviderUnavailableException exception)
        {
            throw new StockFinancialDataUnavailableException(
                reference.SecurityCode,
                exception);
        }

        if (financialData is null)
        {
            throw new StockFinancialDataUnavailableException(reference.SecurityCode);
        }

        var snapshotRepository = uow.Get<FinancialSnapshot>();
        var existingSnapshots = await snapshotRepository
            .GetQueryable(asNoTracking: true)
            .Where(snapshot => snapshot.SecurityId == security.Id)
            .ToListAsync(cancellationToken);
        var existingByDate = existingSnapshots.ToDictionary(
            snapshot => snapshot.DataAsOfDate);
        var seenDates = new HashSet<DateOnly>();
        var newSnapshots = new List<FinancialSnapshot>();
        var results = new List<StockFinancialSnapshotResult>(financialData.Count);

        foreach (var data in financialData)
        {
            if (data is null || !MatchesReference(reference, data))
            {
                throw new StockFinancialDataUnavailableException(reference.SecurityCode);
            }

            if (!seenDates.Add(data.DataAsOfDate))
            {
                throw new StockFinancialDataUnavailableException(reference.SecurityCode);
            }

            if (existingByDate.TryGetValue(data.DataAsOfDate, out var existingSnapshot))
            {
                results.Add(ToResult(existingSnapshot, reference));
                continue;
            }

            var snapshot = CreateSnapshot(security.Id, data);
            newSnapshots.Add(snapshot);
            results.Add(ToResult(snapshot, reference));
            existingByDate.Add(data.DataAsOfDate, snapshot);
        }

        foreach (var snapshot in newSnapshots)
        {
            await snapshotRepository.AddAsync(snapshot, cancellationToken);
        }

        if (newSnapshots.Count > 0)
        {
            await uow.CommitAsync(cancellationToken);
        }

        return results;
    }

    private static bool MatchesReference(
        AShareReference reference,
        StockFinancialData data)
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

    private static FinancialSnapshot CreateSnapshot(
        Guid securityId,
        StockFinancialData data)
    {
        try
        {
            return FinancialSnapshot.Create(
                securityId,
                data.DataAsOfDate,
                data.CapturedAt,
                data.PublishedAt,
                data.EarningsPerShare,
                data.DividendPayoutRatio,
                data.ThreeYearAverageDividendPayoutRatio,
                data.PriceToBookRatio,
                data.ReturnOnEquity,
                data.DataSource,
                data.SourceRecordId,
                data.DataQualityCode);
        }
        catch (ArgumentException exception)
        {
            throw new StockFinancialDataUnavailableException(
                data.SecurityCode,
                exception);
        }
    }

    private static StockFinancialSnapshotResult ToResult(
        FinancialSnapshot snapshot,
        AShareReference reference)
        => new(
            snapshot.Id,
            reference.SecurityCode,
            reference.ExchangeCode,
            snapshot.DataAsOfDate,
            snapshot.CapturedAt,
            snapshot.PublishedAt,
            snapshot.EarningsPerShare,
            snapshot.DividendPayoutRatio,
            snapshot.ThreeYearAverageDividendPayoutRatio,
            snapshot.PriceToBookRatio,
            snapshot.ReturnOnEquity,
            snapshot.DataSource,
            snapshot.SourceRecordId,
            snapshot.DataQualityCode);
}
