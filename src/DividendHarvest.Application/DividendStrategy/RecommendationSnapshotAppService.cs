using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;

namespace DividendHarvest.Application.DividendStrategy;

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
        var modelRunId = Guid.NewGuid();
        var snapshots = new List<RecommendationSnapshot>(recommendation.Stocks.Count);

        foreach (var stock in recommendation.Stocks)
        {
            var analysis = stock.Analysis;
            if (analysis.SecurityId == Guid.Empty)
            {
                throw ApplicationErrors.WithSecurityReference(
                    ApplicationErrorCodes.StockNotConfigured,
                    analysis.SecurityCode,
                    analysis.ExchangeCode);
            }

            snapshots.Add(RecommendationSnapshot.Create(
                modelRunId,
                recommendation.PortfolioId,
                analysis.SecurityId,
                analysis.DataAsOfDate,
                analysis.ClosePrice,
                analysis.ModelDividendPerShare,
                analysis.DividendModeCode,
                analysis.ModelStatusCode,
                analysis.DividendReliabilityCode,
                analysis.ObservedPriceZoneCode,
                analysis.PriceZoneCode,
                analysis.PriceZoneConfirmed,
                analysis.RecommendationCode,
                analysis.DividendYield,
                stock.SuggestedBuyShares,
                stock.SuggestedSellShares,
                stock.SuggestedTradeAmount,
                stock.EstimatedTransactionFeeAmount,
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
