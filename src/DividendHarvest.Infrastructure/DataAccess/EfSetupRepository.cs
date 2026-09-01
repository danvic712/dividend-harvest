using DividendHarvest.Application.Ports;
using DividendHarvest.Infrastructure.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure.DataAccess;

public sealed class EfSetupRepository(DividendHarvestDbContext dbContext) : ISetupRepository
{
    public Task<bool> IsSetupCompletedAsync(CancellationToken cancellationToken)
        => dbContext.Portfolios.AnyAsync(cancellationToken);

    public async Task AddPortfolioAsync(PortfolioRecord portfolio, CancellationToken cancellationToken)
    {
        await dbContext.Portfolios.AddAsync(
            new PortfolioEntity
            {
                Id = portfolio.Id,
                Name = portfolio.Name
            },
            cancellationToken);
    }

    public async Task AddSecurityAsync(SecurityRecord security, CancellationToken cancellationToken)
    {
        await dbContext.Securities.AddAsync(
            new SecurityEntity
            {
                Id = security.Id,
                SecurityCode = security.SecurityCode,
                ExchangeCode = security.ExchangeCode,
                SecurityName = security.SecurityName,
                MarketCode = security.MarketCode,
                CurrencyCode = security.CurrencyCode
            },
            cancellationToken);
    }

    public async Task AddPositionAsync(PositionRecord position, CancellationToken cancellationToken)
    {
        await dbContext.PortfolioPositions.AddAsync(
            new PortfolioPositionEntity
            {
                PortfolioId = position.PortfolioId,
                SecurityId = position.SecurityId,
                HeldShares = position.HeldShares,
                CoreShares = position.CoreShares,
                TargetShares = position.TargetShares,
                AverageCostPerShare = position.AverageCostPerShare
            },
            cancellationToken);
    }
}
