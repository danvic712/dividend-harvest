using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Mapping;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using PortfolioEntity = DividendHarvest.Domain.Models.Portfolio;
using DividendHarvest.Domain.Portfolio;
using DividendHarvest.Domain.Securities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Portfolio;

public sealed class BudgetAppService(
    IUow uow,
    IValidator<RecordCashLedgerEntryRequest> requestValidator,
    TimeProvider timeProvider) : IBudgetAppService
{
    public async Task<CashLedgerEntryResult> RecordAsync(
        RecordCashLedgerEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationResult = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new BudgetValidationException(
                ValidationErrorFormatter.Format(validationResult));
        }

        var portfolio = await uow.Get<PortfolioEntity>()
            .GetQueryable(asNoTracking: true)
            .SingleOrDefaultAsync(cancellationToken);
        if (portfolio is null)
        {
            throw new SetupNotCompletedException();
        }

        var reference = string.IsNullOrWhiteSpace(request.SecurityCode)
            ? null
            : AShareReference.Create(request.SecurityCode!, request.ExchangeCode!);
        var security = reference is null
            ? null
            : await uow.Get<Security>()
                .GetQueryable(asNoTracking: true)
                .SingleOrDefaultAsync(
                    item =>
                        item.SecurityCode == reference.SecurityCode
                        && item.ExchangeCode == reference.ExchangeCode,
                    cancellationToken);
        if (reference is not null && security is null)
        {
            throw new StockNotConfiguredException(
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        var sourceRecordId = request.SourceRecordId?.Trim();
        var ledgerRepository = uow.Get<CashLedgerEntry>();
        if (!string.IsNullOrWhiteSpace(sourceRecordId))
        {
            var existingEntry = await ledgerRepository
                .GetQueryable(asNoTracking: true)
                .SingleOrDefaultAsync(
                    entry => entry.PortfolioId == portfolio.Id
                        && entry.SourceRecordId == sourceRecordId,
                    cancellationToken);
            if (existingEntry is not null)
            {
                if (!MatchesRequest(existingEntry, request, security?.Id))
                {
                    throw new CashLedgerEntryConflictException(sourceRecordId);
                }

                return ApplicationMapper.ToCashLedgerEntryResult(
                    existingEntry,
                    reference?.SecurityCode,
                    reference?.ExchangeCode);
            }
        }

        CashLedgerEntry entry;
        try
        {
            entry = CashLedgerEntry.Create(
                portfolio.Id,
                security?.Id,
                request.EntryDate,
                request.EntryTypeCode,
                request.CashDirectionCode,
                request.CashAmount,
                request.SourceRecordId);
        }
        catch (ArgumentException exception)
        {
            throw new BudgetValidationException(exception.Message);
        }

        await ledgerRepository.AddAsync(entry, cancellationToken);
        try
        {
            await uow.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(sourceRecordId))
        {
            // The filtered unique index protects against two concurrent retries
            // that both pass the read-before-insert idempotency check.
            throw new CashLedgerEntryConflictException(sourceRecordId);
        }

        return ApplicationMapper.ToCashLedgerEntryResult(
            entry,
            reference?.SecurityCode,
            reference?.ExchangeCode);
    }

    public async Task<BudgetSummary> GetSummaryAsync(
        CancellationToken cancellationToken)
    {
        var portfolio = await uow.Get<PortfolioEntity>()
            .GetQueryable(asNoTracking: true)
            .SingleOrDefaultAsync(cancellationToken);
        if (portfolio is null)
        {
            throw new SetupNotCompletedException();
        }

        var entries = await uow.Get<CashLedgerEntry>()
            .GetQueryable(asNoTracking: true)
            .Where(entry => entry.PortfolioId == portfolio.Id)
            .ToListAsync(cancellationToken);
        var totalInflow = entries
            .Where(entry => entry.CashDirectionCode == "inflow")
            .Sum(entry => entry.CashAmount);
        var totalOutflow = entries
            .Where(entry => entry.CashDirectionCode == "outflow")
            .Sum(entry => entry.CashAmount);

        return new BudgetSummary(
            portfolio.Id,
            portfolio.Name,
            totalInflow,
            totalOutflow,
            PortfolioBudgetCalculator.CalculateCashBalance(entries),
            entries.Count,
            timeProvider.GetUtcNow());
    }

    private static bool MatchesRequest(
        CashLedgerEntry entry,
        RecordCashLedgerEntryRequest request,
        Guid? securityId)
        => entry.EntryDate == request.EntryDate
            && entry.EntryTypeCode == NormalizeCode(request.EntryTypeCode)
            && entry.CashDirectionCode == NormalizeCode(request.CashDirectionCode)
            && entry.CashAmount == request.CashAmount
            && entry.SecurityId == securityId;

    private static string NormalizeCode(string value)
        => value.Trim().ToLowerInvariant();
}
