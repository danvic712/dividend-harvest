using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.DividendStrategy;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class RecommendationSnapshotAppServiceTests
{
    [Fact]
    public async Task CreateAsync_persists_all_stock_results_in_one_commit()
    {
        var portfolioId = Guid.NewGuid();
        var security = new Security
        {
            Id = Guid.NewGuid(),
            SecurityCode = "000001",
            ExchangeCode = "SZSE",
            SecurityName = "平安银行",
            MarketCode = "A-share",
            CurrencyCode = "CNY"
        };
        var analysis = CreateAnalysis(security);
        var portfolioRecommendation = new PortfolioRecommendationResult(
            portfolioId,
            1000m,
            200m,
            800m,
            5m,
            [analysis],
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));
        var portfolioRecommendationAppService =
            new Mock<IPortfolioRecommendationAppService>();
        portfolioRecommendationAppService
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolioRecommendation);
        var securityRepository = CreateRepository([security]);
        var snapshotRepository = CreateRepository<RecommendationSnapshot>([]);
        snapshotRepository
            .Setup(x => x.AddAsync(
                It.IsAny<RecommendationSnapshot>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<RecommendationSnapshot>())
            .Returns(snapshotRepository.Object);
        var service = new RecommendationSnapshotAppService(
            unitOfWork.Object,
            portfolioRecommendationAppService.Object,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 1, 0, TimeSpan.Zero)));

        var result = await service.CreateAsync(CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.ModelRunId);
        Assert.Equal(portfolioId, result.PortfolioId);
        Assert.Equal(1, result.SnapshotCount);
        snapshotRepository.Verify(x => x.AddAsync(
            It.Is<RecommendationSnapshot>(snapshot =>
                snapshot.ModelRunId == result.ModelRunId
                && snapshot.PortfolioId == portfolioId
                && snapshot.SecurityId == security.Id
                && snapshot.SuggestedBuyShares == analysis.SuggestedBuyShares),
            It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_does_not_commit_when_analysis_stock_is_not_configured()
    {
        var portfolioRecommendationAppService =
            new Mock<IPortfolioRecommendationAppService>();
        portfolioRecommendationAppService
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortfolioRecommendationResult(
                Guid.NewGuid(),
                0m,
                0m,
                0m,
                0m,
                [CreateAnalysis(new Security
                {
                    Id = Guid.NewGuid(),
                    SecurityCode = "000001",
                    ExchangeCode = "SZSE",
                    SecurityName = "平安银行",
                    MarketCode = "A-share",
                    CurrencyCode = "CNY"
                })],
                DateTimeOffset.UtcNow));
        var unitOfWork = new Mock<IUow>();
        unitOfWork
            .Setup(x => x.Get<Security>())
            .Returns(CreateRepository<Security>([]).Object);
        var service = new RecommendationSnapshotAppService(
            unitOfWork.Object,
            portfolioRecommendationAppService.Object,
            TimeProvider.System);

        await Assert.ThrowsAsync<ApplicationErrorException>(() =>
            service.CreateAsync(CancellationToken.None));

        unitOfWork.Verify(x => x.Get<RecommendationSnapshot>(), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static StockAnalysisResult CreateAnalysis(Security security)
        => new(
            security.SecurityCode,
            security.ExchangeCode,
            security.SecurityName,
            "available",
            "passed",
            4m,
            0.4m,
            "ttm",
            0.1m,
            5m,
            6.66m,
            10m,
            13.33m,
            "strong_buy",
            "strong_buy",
            true,
            "strong_buy",
            0,
            0,
            0,
            200,
            0,
            800m,
            5m,
            new DateOnly(2026, 9, 1),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
            "测试结果");

    private static Mock<IRepository<TEntity>> CreateRepository<TEntity>(
        IEnumerable<TEntity> entities)
        where TEntity : class
        => RepositoryMock.Create(entities);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
