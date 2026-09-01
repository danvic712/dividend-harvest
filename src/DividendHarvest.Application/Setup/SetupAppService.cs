using DividendHarvest.Application.Ports;
using DividendHarvest.Domain.Portfolio;
using DividendHarvest.Domain.Securities;

namespace DividendHarvest.Application.Setup;

public sealed class SetupAppService(
    ISetupRepository repository,
    IStockDataProvider stockDataProvider,
    IUnitOfWork unitOfWork) : ISetupAppService
{
    public async Task<SetupStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var isComplete = await repository.IsSetupCompletedAsync(cancellationToken);
        return isComplete
            ? new SetupStatus(true, [])
            : new SetupStatus(false, ["portfolio", "stocks"]);
    }

    public async Task<SetupResult> InitializeAsync(
        SetupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await repository.IsSetupCompletedAsync(cancellationToken))
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
            var stockData = await stockDataProvider.GetAsync(reference, cancellationToken)
                ?? throw new StockDataUnavailableException(reference.SecurityCode);

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

        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await repository.AddPortfolioAsync(
                new PortfolioRecord(portfolioId, portfolioName),
                transactionCancellationToken);

            foreach (var stock in resolvedStocks)
            {
                await repository.AddSecurityAsync(
                    new SecurityRecord(
                        stock.SecurityId,
                        stock.Reference.SecurityCode,
                        stock.Reference.ExchangeCode,
                        stock.Data.SecurityName,
                        stock.Data.MarketCode,
                        stock.Data.CurrencyCode),
                    transactionCancellationToken);

                if (stock.InitialHolding is not null)
                {
                    await repository.AddPositionAsync(
                        new PositionRecord(
                            portfolioId,
                            stock.SecurityId,
                            stock.InitialHolding.HeldShares,
                            stock.InitialHolding.CoreShares,
                            stock.InitialHolding.TargetShares,
                            stock.InitialHolding.AverageCostPerShare),
                        transactionCancellationToken);
                }
            }
        }, cancellationToken);

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
