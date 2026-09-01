using DividendHarvest.Domain.Portfolio;

namespace DividendHarvest.Application.Ports;

public interface ISetupRepository
{
    Task<bool> IsSetupCompletedAsync(CancellationToken cancellationToken);

    Task AddPortfolioAsync(PortfolioRecord portfolio, CancellationToken cancellationToken);

    Task AddSecurityAsync(SecurityRecord security, CancellationToken cancellationToken);

    Task AddPositionAsync(PositionRecord position, CancellationToken cancellationToken);
}

public interface IStockDataProvider
{
    Task<StockData?> GetAsync(
        DividendHarvest.Domain.Securities.AShareReference reference,
        CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}

public sealed record PortfolioRecord(Guid Id, string Name);

public sealed record SecurityRecord(
    Guid Id,
    string SecurityCode,
    string ExchangeCode,
    string SecurityName,
    string MarketCode,
    string CurrencyCode);

public sealed record PositionRecord(
    Guid PortfolioId,
    Guid SecurityId,
    int HeldShares,
    int CoreShares,
    int TargetShares,
    decimal AverageCostPerShare);

public sealed record StockData(
    string SecurityCode,
    string ExchangeCode,
    string SecurityName,
    string MarketCode,
    string CurrencyCode);
