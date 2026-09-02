using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using PortfolioEntity = DividendHarvest.Domain.Models.Portfolio;
using DividendHarvest.Domain.Securities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.DividendStrategy;

public sealed class StockModelParameterAppService(
    IUow uow,
    IValidator<SaveStockModelParametersRequest> saveRequestValidator,
    IValidator<GetStockModelParametersRequest> getRequestValidator,
    TimeProvider timeProvider) : IStockModelParameterAppService
{
    public async Task<StockModelParameterSet?> GetAsync(
        GetStockModelParametersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationResult = await getRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ModelParameterValidationException(
                ValidationErrorFormatter.Format(validationResult));
        }

        var reference = AShareReference.Create(request.SecurityCode, request.ExchangeCode);
        var security = await FindSecurityAsync(reference, cancellationToken);
        if (security is null)
        {
            throw new StockNotConfiguredException(
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var parameters = await uow.Get<ModelParameterSet>()
            .GetQueryable(asNoTracking: true)
            .Where(parameter =>
                parameter.SecurityId == security.Id
                && parameter.EffectiveFromDate <= currentDate)
            .OrderByDescending(parameter => parameter.EffectiveFromDate)
            .FirstOrDefaultAsync(cancellationToken);

        return parameters is null
            ? null
            : ToDto(parameters, reference);
    }

    public async Task<StockModelParameterSet> SaveAsync(
        SaveStockModelParametersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationResult = await saveRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ModelParameterValidationException(
                ValidationErrorFormatter.Format(validationResult));
        }

        var reference = AShareReference.Create(request.SecurityCode, request.ExchangeCode);
        var security = await FindSecurityAsync(reference, cancellationToken);
        if (security is null)
        {
            throw new StockNotConfiguredException(
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        var portfolio = await uow.Get<PortfolioEntity>()
            .GetQueryable(asNoTracking: true)
            .FirstOrDefaultAsync(cancellationToken);
        if (portfolio is null)
        {
            throw new SetupNotCompletedException();
        }

        var parameterRepository = uow.Get<ModelParameterSet>();
        var versionExists = await parameterRepository
            .GetQueryable(asNoTracking: true)
            .AnyAsync(parameter =>
                parameter.SecurityId == security.Id
                && parameter.EffectiveFromDate == request.EffectiveFromDate,
                cancellationToken);
        if (versionExists)
        {
            throw new ModelParameterVersionAlreadyExistsException(
                reference.SecurityCode,
                request.EffectiveFromDate);
        }

        var parameters = CreateParameters(portfolio.Id, security.Id, request);
        await parameterRepository.AddAsync(parameters, cancellationToken);
        await uow.CommitAsync(cancellationToken);

        return ToDto(parameters, reference);
    }

    private async Task<Security?> FindSecurityAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
        => await uow.Get<Security>()
            .GetQueryable(asNoTracking: true)
            .SingleOrDefaultAsync(
                security =>
                    security.SecurityCode == reference.SecurityCode
                    && security.ExchangeCode == reference.ExchangeCode,
                cancellationToken);

    private static ModelParameterSet CreateParameters(
        Guid portfolioId,
        Guid securityId,
        SaveStockModelParametersRequest request)
    {
        try
        {
            return ModelParameterSet.Create(
                portfolioId,
                securityId,
                request.ModelVersion,
                request.StrongBuyYieldThreshold,
                request.AccumulationYieldThreshold,
                request.PartialTrimYieldThreshold,
                request.AggressiveTrimYieldThreshold,
                request.StrongBuyBudgetRatio,
                request.AccumulateBudgetRatio,
                request.PartialTrimRatio,
                request.AggressiveTrimRatio,
                request.MaxSecurityWeight,
                request.MaxSectorWeight,
                request.CashReserveRatio,
                request.MaxSingleTradeAmount,
                request.MaxPeriodBudgetAmount,
                request.TransactionFeeRatio,
                request.MinimumTransactionFeeAmount,
                request.TradingLotSize,
                request.EffectiveFromDate);
        }
        catch (ArgumentException exception)
        {
            throw new ModelParameterValidationException(exception.Message);
        }
    }

    private static StockModelParameterSet ToDto(
        ModelParameterSet parameters,
        AShareReference reference)
        => new(
            parameters.Id,
            reference.SecurityCode,
            reference.ExchangeCode,
            parameters.ModelVersion,
            parameters.StrongBuyYieldThreshold,
            parameters.AccumulationYieldThreshold,
            parameters.PartialTrimYieldThreshold,
            parameters.AggressiveTrimYieldThreshold,
            parameters.StrongBuyBudgetRatio,
            parameters.AccumulateBudgetRatio,
            parameters.PartialTrimRatio,
            parameters.AggressiveTrimRatio,
            parameters.MaxSecurityWeight,
            parameters.MaxSectorWeight,
            parameters.CashReserveRatio,
            parameters.MaxSingleTradeAmount,
            parameters.MaxPeriodBudgetAmount,
            parameters.TransactionFeeRatio,
            parameters.MinimumTransactionFeeAmount,
            parameters.TradingLotSize,
            parameters.EffectiveFromDate);
}
