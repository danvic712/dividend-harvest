using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dto;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Portfolio;
using DividendHarvest.Domain.Securities;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Setup;

public sealed class SetupAppService(
    IUow uow,
    IStockDataProvider stockDataProvider) : ISetupAppService
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

        if (await uow.Get<PortfolioEntity>()
            .GetQueryable(asNoTracking: true)
            .AnyAsync(cancellationToken))
        {
            throw new SetupAlreadyCompletedException();
        }

        var portfolioName = request.PortfolioName?.Trim() ?? string.Empty;
        if (portfolioName.Length is < 1 or > 100)
        {
            throw new SetupValidationException("投资组合名称必须为 1 到 100 个字符。");
        }

        if (request.Stocks is null || request.Stocks.Count == 0)
        {
            throw new SetupValidationException("至少需要配置一只 A 股股票。");
        }

        var references = request.Stocks
            .Select(stock =>
            {
                try
                {
                    return AShareReference.Create(stock.SecurityCode, stock.ExchangeCode);
                }
                catch (ArgumentException exception)
                {
                    throw new SetupValidationException(exception.Message);
                }
            })
            .ToArray();

        if (references.Distinct().Count() != references.Length)
        {
            throw new SetupValidationException("不能重复配置同一只股票。");
        }

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
            catch (StockDataProviderUnavailableException exception)
            {
                throw new StockDataUnavailableException(reference.SecurityCode, exception);
            }

            if (stockData is null)
            {
                throw new StockDataUnavailableException(reference.SecurityCode);
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
        var securityRepository = uow.Get<SecurityEntity>();
        var positionRepository = uow.Get<PortfolioPositionEntity>();

        await portfolioRepository.AddAsync(
            new PortfolioEntity
            {
                Id = portfolioId,
                Name = portfolioName
            },
            cancellationToken);

        foreach (var stock in resolvedStocks)
        {
            await securityRepository.AddAsync(
                new SecurityEntity
                {
                    Id = stock.SecurityId,
                    SecurityCode = stock.Reference.SecurityCode,
                    ExchangeCode = stock.Reference.ExchangeCode,
                    SecurityName = stock.Data.SecurityName,
                    MarketCode = stock.Data.MarketCode,
                    CurrencyCode = stock.Data.CurrencyCode
                },
                cancellationToken);

            if (stock.InitialHolding is not null)
            {
                await positionRepository.AddAsync(
                    new PortfolioPositionEntity
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
                .Select(stock => new SetupStockResult(
                    stock.Reference.SecurityCode,
                    stock.Reference.ExchangeCode,
                    stock.Data.SecurityName))
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
            throw new SetupValidationException(exception.Message);
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
            throw new StockDataUnavailableException(reference.SecurityCode);
        }
    }

    private sealed record ResolvedStock(
        Guid SecurityId,
        AShareReference Reference,
        StockData Data,
        InitialHolding? InitialHolding);
}
