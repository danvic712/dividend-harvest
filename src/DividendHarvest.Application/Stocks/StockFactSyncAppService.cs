using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Localization;
using DividendHarvest.Application.Mapping;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;

namespace DividendHarvest.Application.Stocks;

public sealed class StockFactSyncAppService(
    IUow uow,
    IStockDataProvider stockDataProvider,
    IApplicationErrorCatalog applicationErrorCatalog) : IStockFactSyncAppService
{
    private static readonly string[] DataKinds = ["price", "dividend", "financial"];

    public async Task<StockFactSyncResult> SyncAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
    {
        var failures = new List<StockDataSyncFailure>();
        var security = await FindSecurityAsync(reference, cancellationToken);
        if (security is null)
        {
            var exception = ApplicationErrors.WithSecurityReference(
                ApplicationErrorCodes.StockNotConfigured,
                reference.SecurityCode,
                reference.ExchangeCode);
            foreach (var dataKind in DataKinds)
            {
                failures.Add(ToFailure(reference, dataKind, exception));
            }

            return new StockFactSyncResult(
                reference.SecurityCode,
                reference.ExchangeCode,
                null,
                [],
                [],
                failures);
        }

        StockPriceObservationResult? priceObservation = null;
        var dividendEvents = Array.Empty<StockDividendEventResult>();
        var financialSnapshots = Array.Empty<StockFinancialSnapshotResult>();

        await TrySyncAsync(
            reference,
            "price",
            async () => priceObservation = await SyncPriceAsync(
                reference,
                security,
                cancellationToken),
            failures,
            cancellationToken);
        await TrySyncAsync(
            reference,
            "dividend",
            async () => dividendEvents = (await SyncDividendsAsync(
                reference,
                security,
                cancellationToken)).ToArray(),
            failures,
            cancellationToken);
        await TrySyncAsync(
            reference,
            "financial",
            async () => financialSnapshots = (await SyncFinancialsAsync(
                reference,
                security,
                cancellationToken)).ToArray(),
            failures,
            cancellationToken);

        return new StockFactSyncResult(
            reference.SecurityCode,
            reference.ExchangeCode,
            priceObservation,
            dividendEvents,
            financialSnapshots,
            failures);
    }

    public async Task<StockPriceObservationResult> SyncPriceAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
    {
        var security = await GetSecurityAsync(reference, cancellationToken);
        return await SyncPriceAsync(reference, security, cancellationToken);
    }

    public async Task<IReadOnlyList<StockDividendEventResult>> SyncDividendsAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
    {
        var security = await GetSecurityAsync(reference, cancellationToken);
        return await SyncDividendsAsync(reference, security, cancellationToken);
    }

    public async Task<IReadOnlyList<StockFinancialSnapshotResult>> SyncFinancialsAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
    {
        var security = await GetSecurityAsync(reference, cancellationToken);
        return await SyncFinancialsAsync(reference, security, cancellationToken);
    }

    private async Task<Security?> FindSecurityAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
        => await uow.Get<Security>().SingleOrDefaultAsync(
            item => item.SecurityCode == reference.SecurityCode
                && item.ExchangeCode == reference.ExchangeCode,
            cancellationToken);

    private async Task<Security> GetSecurityAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
        => await FindSecurityAsync(reference, cancellationToken)
            ?? throw ApplicationErrors.WithSecurityReference(
                ApplicationErrorCodes.StockNotConfigured,
                reference.SecurityCode,
                reference.ExchangeCode);

    private async Task<StockPriceObservationResult> SyncPriceAsync(
        AShareReference reference,
        Security security,
        CancellationToken cancellationToken)
    {
        StockMarketData? marketData;
        try
        {
            marketData = await stockDataProvider.GetMarketDataAsync(
                reference,
                cancellationToken);
        }
        catch (Exception exception) when (exception is IStockDataProviderFailure)
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockMarketDataUnavailable,
                reference.SecurityCode,
                exception);
        }

        if (marketData is null || !MatchesReference(reference, marketData))
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockMarketDataUnavailable,
                reference.SecurityCode);
        }

        var existingObservation = await uow.Get<PriceObservation>()
            .SingleOrDefaultAsync(
                observation => observation.SecurityId == security.Id
                    && observation.TradingDate == marketData.TradingDate,
                cancellationToken);
        if (existingObservation is not null)
        {
            return ApplicationMapper.ToStockPriceObservationResult(
                existingObservation,
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        PriceObservation observation;
        try
        {
            observation = PriceObservation.Create(
                security.Id,
                marketData.TradingDate,
                marketData.ClosePrice,
                marketData.PriceObservedAt,
                marketData.DataSource,
                marketData.SourceRecordId,
                marketData.DataQualityCode);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockMarketDataUnavailable,
                reference.SecurityCode,
                exception);
        }

        await uow.Get<PriceObservation>().AddAsync(observation, cancellationToken);
        await uow.CommitAsync(cancellationToken);

        return ApplicationMapper.ToStockPriceObservationResult(
            observation,
            reference.SecurityCode,
            reference.ExchangeCode);
    }

    private async Task<IReadOnlyList<StockDividendEventResult>> SyncDividendsAsync(
        AShareReference reference,
        Security security,
        CancellationToken cancellationToken)
    {
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
        var existingEvents = await eventRepository.ListAsync(
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
            if (data is null
                || !MatchesReference(reference, data)
                || !seenSourceRecordIds.Add(data.SourceRecordId))
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

            DividendEvent dividendEvent;
            try
            {
                dividendEvent = DividendEvent.Create(
                    security.Id,
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
                    reference.SecurityCode,
                    exception);
            }

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

    private async Task<IReadOnlyList<StockFinancialSnapshotResult>> SyncFinancialsAsync(
        AShareReference reference,
        Security security,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StockFinancialData>? financialData;
        try
        {
            financialData = await stockDataProvider.GetFinancialSnapshotsAsync(
                reference,
                cancellationToken);
        }
        catch (Exception exception) when (exception is IStockDataProviderFailure)
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockFinancialDataUnavailable,
                reference.SecurityCode,
                exception);
        }

        if (financialData is null)
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockFinancialDataUnavailable,
                reference.SecurityCode);
        }

        var snapshotRepository = uow.Get<FinancialSnapshot>();
        var existingSnapshots = await snapshotRepository.ListAsync(
            snapshot => snapshot.SecurityId == security.Id,
            cancellationToken: cancellationToken);
        var existingByDate = existingSnapshots.ToDictionary(
            snapshot => snapshot.DataAsOfDate);
        var seenDates = new HashSet<DateOnly>();
        var newSnapshots = new List<FinancialSnapshot>();
        var results = new List<StockFinancialSnapshotResult>(financialData.Count);

        foreach (var data in financialData)
        {
            if (data is null
                || !MatchesReference(reference, data)
                || !seenDates.Add(data.DataAsOfDate))
            {
                throw ApplicationErrors.WithSecurity(
                    ApplicationErrorCodes.StockFinancialDataUnavailable,
                    reference.SecurityCode);
            }

            if (existingByDate.TryGetValue(data.DataAsOfDate, out var existingSnapshot))
            {
                results.Add(ApplicationMapper.ToStockFinancialSnapshotResult(
                    existingSnapshot,
                    reference.SecurityCode,
                    reference.ExchangeCode));
                continue;
            }

            FinancialSnapshot snapshot;
            try
            {
                snapshot = FinancialSnapshot.Create(
                    security.Id,
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
                throw ApplicationErrors.WithSecurity(
                    ApplicationErrorCodes.StockFinancialDataUnavailable,
                    reference.SecurityCode,
                    exception);
            }

            newSnapshots.Add(snapshot);
            results.Add(ApplicationMapper.ToStockFinancialSnapshotResult(
                snapshot,
                reference.SecurityCode,
                reference.ExchangeCode));
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

    private async Task TrySyncAsync(
        AShareReference reference,
        string dataKind,
        Func<Task> sync,
        ICollection<StockDataSyncFailure> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            await sync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsExpectedSyncFailure(exception))
        {
            var applicationException = (ApplicationExceptionBase)exception;
            failures.Add(ToFailure(reference, dataKind, applicationException));
        }
    }

    private StockDataSyncFailure ToFailure(
        AShareReference reference,
        string dataKind,
        ApplicationExceptionBase exception)
    {
        var localizedError = applicationErrorCatalog.Resolve(exception);
        return new StockDataSyncFailure(
            reference.SecurityCode,
            reference.ExchangeCode,
            dataKind,
            exception.ErrorCode,
            localizedError.Detail);
    }

    private static bool IsExpectedSyncFailure(Exception exception)
        => exception is ApplicationExceptionBase applicationException
            && ApplicationErrorCodes.ExpectedStockSyncFailures.Contains(
                applicationException.ErrorCode,
                StringComparer.Ordinal);

    private static bool MatchesReference(
        AShareReference reference,
        StockMarketData marketData)
        => MatchesReference(reference, marketData.SecurityCode, marketData.ExchangeCode);

    private static bool MatchesReference(
        AShareReference reference,
        StockDividendData data)
        => MatchesReference(reference, data.SecurityCode, data.ExchangeCode);

    private static bool MatchesReference(
        AShareReference reference,
        StockFinancialData data)
        => MatchesReference(reference, data.SecurityCode, data.ExchangeCode);

    private static bool MatchesReference(
        AShareReference reference,
        string securityCode,
        string exchangeCode)
    {
        try
        {
            return AShareReference.Create(securityCode, exchangeCode) == reference;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
