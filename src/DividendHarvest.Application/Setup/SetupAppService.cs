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

namespace DividendHarvest.Application.Setup;

public sealed class SetupAppService(
    IUow uow,
    IStockDataProvider stockDataProvider,
    IValidator<SetupRequest> requestValidator) : ISetupAppService
{
    public async Task<SetupStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var isComplete = await uow.Get<PortfolioEntity>()
            .GetQueryable(asNoTracking: true)
            .AnyAsync(cancellationToken);

        return isComplete
            ? new SetupStatus(true, [])
            : new SetupStatus(false, ["portfolio", "stocks"]);
    }

    public async Task<SetupResult> InitializeAsync(
        SetupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw ApplicationErrors.Validation(
                ApplicationErrorCodes.SetupValidationFailed,
                ValidationErrorFormatter.Format(validationResult));
        }

        if (await uow.Get<PortfolioEntity>()
            .GetQueryable(asNoTracking: true)
            .AnyAsync(cancellationToken))
        {
            throw ApplicationErrors.Simple(ApplicationErrorCodes.SetupAlreadyCompleted);
        }

        var portfolioName = request.PortfolioName.Trim();

        var references = request.Stocks
            .Select(stock =>
            {
                try
                {
                    return AShareReference.Create(stock.SecurityCode, stock.ExchangeCode);
                }
                catch (ArgumentException exception)
                {
                    throw ApplicationErrors.Validation(
                        ApplicationErrorCodes.SetupValidationFailed,
                        exception.Message);
                }
            })
            .ToArray();

        var portfolioId = Guid.NewGuid();
        var resolvedStocks = new List<ResolvedStock>(request.Stocks.Count);

        for (var index = 0; index < request.Stocks.Count; index++)
        {
            var stock = request.Stocks[index];
            var reference = references[index];
            StockData? stockData;
            try
            {
                stockData = await stockDataProvider.GetAsync(reference, cancellationToken);
            }
            catch (Exception exception) when (exception is IStockDataProviderFailure)
            {
                throw ApplicationErrors.WithSecurity(
                    ApplicationErrorCodes.StockDataUnavailable,
                    reference.SecurityCode,
                    exception);
            }

            if (stockData is null)
            {
                throw ApplicationErrors.WithSecurity(
                    ApplicationErrorCodes.StockDataUnavailable,
                    reference.SecurityCode);
            }

            ValidateStockData(reference, stockData);
            var initialHolding = stock.InitialHolding is null
                ? null
                : CreateInitialHolding(stock.InitialHolding);

            resolvedStocks.Add(new ResolvedStock(
                Guid.NewGuid(),
                reference,
                stockData,
                initialHolding));
        }

        var portfolioRepository = uow.Get<PortfolioEntity>();
        var securityRepository = uow.Get<Security>();
        var positionRepository = uow.Get<PortfolioPosition>();

        await portfolioRepository.AddAsync(
                new PortfolioEntity
                {
                    Id = portfolioId,
                    Name = portfolioName,
                    CurrencyCode = "CNY"
                },
            cancellationToken);

        foreach (var stock in resolvedStocks)
        {
            await securityRepository.AddAsync(
                new Security
                {
                    Id = stock.SecurityId,
                    SecurityCode = stock.Reference.SecurityCode,
                    ExchangeCode = stock.Reference.ExchangeCode,
                    SecurityName = stock.Data.SecurityName,
                    MarketCode = stock.Data.MarketCode,
                    CurrencyCode = stock.Data.CurrencyCode,
                    SectorCode = stock.Data.SectorCode
                },
                cancellationToken);

            if (stock.InitialHolding is not null)
            {
                await positionRepository.AddAsync(
                    new PortfolioPosition
                    {
                        PortfolioId = portfolioId,
                        SecurityId = stock.SecurityId,
                        HeldShares = stock.InitialHolding.HeldShares,
                        CoreShares = stock.InitialHolding.CoreShares,
                        TargetShares = stock.InitialHolding.TargetShares,
                        AverageCostPerShare = stock.InitialHolding.AverageCostPerShare
                    },
                    cancellationToken);
            }
        }

        await uow.CommitAsync(cancellationToken);

        return new SetupResult(
            portfolioId,
            portfolioName,
            resolvedStocks
                .Select(stock => ApplicationMapper.ToSetupStockResult(
                    stock.Data,
                    stock.Reference.SecurityCode,
                    stock.Reference.ExchangeCode))
                .ToArray());
    }

    private static InitialHolding CreateInitialHolding(InitialHoldingInput input)
    {
        try
        {
            return InitialHolding.Create(
                input.HeldShares,
                input.CoreShares,
                input.TargetShares,
                input.AverageCostPerShare);
        }
        catch (ArgumentException exception)
        {
            throw ApplicationErrors.Validation(
                ApplicationErrorCodes.SetupValidationFailed,
                exception.Message);
        }
    }

    private static void ValidateStockData(AShareReference reference, StockData stockData)
    {
        if (!string.Equals(stockData.SecurityCode, reference.SecurityCode, StringComparison.Ordinal)
            || !string.Equals(stockData.ExchangeCode, reference.ExchangeCode, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(stockData.SecurityName)
            || string.IsNullOrWhiteSpace(stockData.MarketCode)
            || string.IsNullOrWhiteSpace(stockData.CurrencyCode))
        {
            throw ApplicationErrors.WithSecurity(
                ApplicationErrorCodes.StockDataUnavailable,
                reference.SecurityCode);
        }
    }

    private sealed record ResolvedStock(
        Guid SecurityId,
        AShareReference Reference,
        StockData Data,
        InitialHolding? InitialHolding);
}
