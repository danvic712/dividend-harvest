using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Application.Recommendations;

public sealed class RecommendationSnapshotAppService(
    IUow uow,
    IPortfolioRecommendationAppService portfolioRecommendationAppService,
    TimeProvider timeProvider) : IRecommendationSnapshotAppService
{
    public async Task<CreateRecommendationSnapshotResult> CreateAsync(
        CancellationToken cancellationToken)
    {
        var recommendation = await portfolioRecommendationAppService.GetAsync(
            cancellationToken);
        var stocks = await uow.Get<Security>()
            .GetQueryable(asNoTracking: true)
            .ToListAsync(cancellationToken);
        var securitiesByReference = stocks.ToDictionary(
            security => (security.SecurityCode, security.ExchangeCode));
        var modelRunId = Guid.NewGuid();
        var snapshots = new List<RecommendationSnapshot>(recommendation.Stocks.Count);

        foreach (var analysis in recommendation.Stocks)
        {
            if (!securitiesByReference.TryGetValue(
                    (analysis.SecurityCode, analysis.ExchangeCode),
                    out var security))
            {
                throw new StockNotConfiguredException(
                    analysis.SecurityCode,
                    analysis.ExchangeCode);
            }

            snapshots.Add(RecommendationSnapshot.Create(
                modelRunId,
                recommendation.PortfolioId,
                security.Id,
                analysis.DataAsOfDate,
                analysis.ClosePrice,
                analysis.ModelDividendPerShare,
                analysis.DividendModeCode,
                analysis.ModelStatusCode,
                analysis.DividendReliabilityCode,
                analysis.PriceZoneCode,
                analysis.RecommendationCode,
                analysis.DividendYield,
                analysis.SuggestedBuyShares,
                analysis.SuggestedSellShares,
                analysis.SuggestedTradeAmount,
                analysis.EstimatedTransactionFeeAmount,
                analysis.ComputedAt,
                analysis.ModelParameterSetId));
        }

        var repository = uow.Get<RecommendationSnapshot>();
        foreach (var snapshot in snapshots)
        {
            await repository.AddAsync(snapshot, cancellationToken);
        }

        await uow.CommitAsync(cancellationToken);

        return new CreateRecommendationSnapshotResult(
            modelRunId,
            recommendation.PortfolioId,
            snapshots.Count,
            timeProvider.GetUtcNow(),
            recommendation.Stocks);
    }
}
