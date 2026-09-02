using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Watchlist;

public sealed class StockWatchlistAppService(IUow uow) : IStockWatchlistAppService
{
    public async Task<IReadOnlyList<StockWatchlistItem>> GetAsync(
        CancellationToken cancellationToken)
    {
        var securities = await uow.Get<Security>()
            .GetQueryable(asNoTracking: true)
            .OrderBy(security => security.SecurityCode)
            .ThenBy(security => security.ExchangeCode)
            .ToListAsync(cancellationToken);
        var positions = await uow.Get<PortfolioPosition>()
            .GetQueryable(asNoTracking: true)
            .ToListAsync(cancellationToken);
        var holdingsBySecurityId = positions.ToDictionary(position => position.SecurityId);

        return securities
            .Select(security => new StockWatchlistItem(
                security.SecurityCode,
                security.ExchangeCode,
                security.SecurityName,
                security.MarketCode,
                security.CurrencyCode,
                holdingsBySecurityId.TryGetValue(security.Id, out var position)
                    ? new StockHoldingSnapshot(
                        position.HeldShares,
                        position.CoreShares,
                        position.TargetShares,
                        position.AverageCostPerShare)
                    : null,
                security.SectorCode))
            .ToArray();
    }
}
