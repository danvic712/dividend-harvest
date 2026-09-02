using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.DividendStrategy;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class PortfolioRecommendationAppServiceTests
{
    [Fact]
    public async Task GetAsync_allocates_budget_to_strong_buy_before_accumulate()
    {
        var portfolioId = Guid.NewGuid();
        var firstParameter = CreateParameters(portfolioId);
        var secondParameter = CreateParameters(portfolioId);
        var stocks = new[]
        {
            new StockWatchlistItem(
                "000001",
                "SZSE",
                "平安银行",
                "A-share",
                "CNY",
                null),
            new StockWatchlistItem(
                "600001",
                "SSE",
                "示例银行",
                "A-share",
                "CNY",
                null)
        };
        var firstAnalysis = CreateAnalysis(
            "000001",
            "SZSE",
            "平安银行",
            "strong_buy",
            firstParameter.Id,
            4m);
        var secondAnalysis = CreateAnalysis(
            "600001",
            "SSE",
            "示例银行",
            "accumulate",
            secondParameter.Id,
            5m);
        var watchlistAppService = new Mock<IStockWatchlistAppService>();
        watchlistAppService
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);
        var analysisAppService = new Mock<IStockAnalysisAppService>();
        analysisAppService
            .Setup(x => x.GetAsync(
                It.Is<GetStockAnalysisRequest>(request => request.SecurityCode == "000001"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstAnalysis);
        analysisAppService
            .Setup(x => x.GetAsync(
                It.Is<GetStockAnalysisRequest>(request => request.SecurityCode == "600001"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondAnalysis);
        var budgetAppService = new Mock<IBudgetAppService>();
        budgetAppService
            .Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetSummary(
                portfolioId,
                "长期股息组合",
                5000m,
                0m,
                5000m,
                1,
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));
        var parameterRepository = CreateRepository([firstParameter, secondParameter]);
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<ModelParameterSet>()).Returns(parameterRepository.Object);
        var service = new PortfolioRecommendationAppService(
            unitOfWork.Object,
            watchlistAppService.Object,
            analysisAppService.Object,
            budgetAppService.Object,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal(5000m, result.StartingAvailableBudgetAmount);
        Assert.Equal(600, result.Stocks[0].SuggestedBuyShares);
        Assert.Equal(100, result.Stocks[1].SuggestedBuyShares);
        Assert.Equal(2900m, result.TotalSuggestedTradeAmount);
        Assert.Equal(10m, result.EstimatedTransactionFeeAmount);
        Assert.Equal(2090m, result.RemainingAvailableBudgetAmount);
    }

    [Fact]
    public async Task GetAsync_keeps_cautious_stock_without_a_trade_quantity()
    {
        var portfolioId = Guid.NewGuid();
        var parameter = CreateParameters(portfolioId);
        var stock = new StockWatchlistItem(
            "000001",
            "SZSE",
            "平安银行",
            "A-share",
            "CNY",
            null);
        var analysis = CreateAnalysis(
            "000001",
            "SZSE",
            "平安银行",
            "strong_buy",
            parameter.Id,
            4m,
            "cautious",
            "cautious",
            "no_action");
        var watchlistAppService = new Mock<IStockWatchlistAppService>();
        watchlistAppService
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([stock]);
        var analysisAppService = new Mock<IStockAnalysisAppService>();
        analysisAppService
            .Setup(x => x.GetAsync(
                It.IsAny<GetStockAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        var budgetAppService = new Mock<IBudgetAppService>();
        budgetAppService
            .Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetSummary(
                portfolioId,
                "长期股息组合",
                5000m,
                0m,
                5000m,
                1,
                DateTimeOffset.UtcNow));
        var unitOfWork = new Mock<IUow>();
        unitOfWork
            .Setup(x => x.Get<ModelParameterSet>())
            .Returns(CreateRepository([parameter]).Object);
        var service = new PortfolioRecommendationAppService(
            unitOfWork.Object,
            watchlistAppService.Object,
            analysisAppService.Object,
            budgetAppService.Object,
            TimeProvider.System);

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal(0, result.Stocks[0].SuggestedBuyShares);
        Assert.Equal(0m, result.TotalSuggestedTradeAmount);
    }

    [Fact]
    public async Task GetAsync_does_not_allocate_buy_budget_when_a_held_position_lacks_a_price()
    {
        var portfolioId = Guid.NewGuid();
        var firstParameter = CreateParameters(portfolioId);
        var secondParameter = CreateParameters(portfolioId);
        var stocks = new[]
        {
            new StockWatchlistItem("000001", "SZSE", "平安银行", "A-share", "CNY", null),
            new StockWatchlistItem("600001", "SSE", "示例银行", "A-share", "CNY", null)
        };
        var analyses = new[]
        {
            CreateAnalysis(
                "000001",
                "SZSE",
                "平安银行",
                "strong_buy",
                firstParameter.Id,
                4m,
                heldShares: 100),
            CreateAnalysis(
                "600001",
                "SSE",
                "示例银行",
                "hold",
                secondParameter.Id,
                null,
                recommendationCode: "hold",
                heldShares: 50)
        };
        var watchlistAppService = new Mock<IStockWatchlistAppService>();
        watchlistAppService
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);
        var analysisAppService = new Mock<IStockAnalysisAppService>();
        analysisAppService
            .SetupSequence(x => x.GetAsync(
                It.IsAny<GetStockAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analyses[0])
            .ReturnsAsync(analyses[1]);
        var budgetAppService = new Mock<IBudgetAppService>();
        budgetAppService
            .Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetSummary(
                portfolioId,
                "长期股息组合",
                5000m,
                0m,
                5000m,
                1,
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));
        var unitOfWork = new Mock<IUow>();
        unitOfWork
            .Setup(x => x.Get<ModelParameterSet>())
            .Returns(CreateRepository([firstParameter, secondParameter]).Object);
        var service = new PortfolioRecommendationAppService(
            unitOfWork.Object,
            watchlistAppService.Object,
            analysisAppService.Object,
            budgetAppService.Object,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal(0m, result.StartingAvailableBudgetAmount);
        Assert.Equal(0, result.Stocks[0].SuggestedBuyShares);
        Assert.Equal(0m, result.TotalSuggestedTradeAmount);
    }

    private static StockAnalysisResult CreateAnalysis(
        string securityCode,
        string exchangeCode,
        string securityName,
        string priceZoneCode,
        Guid modelParameterSetId,
        decimal? closePrice,
        string modelStatusCode = "available",
        string reliabilityCode = "passed",
        string recommendationCode = "strong_buy",
        int heldShares = 0)
        => new(
            securityCode,
            exchangeCode,
            securityName,
            modelStatusCode,
            reliabilityCode,
            closePrice,
            0.40m,
            "ttm",
            0.10m,
            5m,
            6.66m,
            10m,
            13.33m,
            priceZoneCode,
            priceZoneCode,
            true,
            recommendationCode,
            heldShares,
            0,
            0,
            0,
            0,
            0m,
            0m,
            new DateOnly(2026, 9, 1),
            modelParameterSetId,
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
            "测试分析结果");

    private static ModelParameterSet CreateParameters(Guid portfolioId)
        => ModelParameterSet.Create(
            portfolioId,
            Guid.NewGuid(),
            "v1",
            0.08m,
            0.06m,
            0.04m,
            0.03m,
            0.5m,
            0.25m,
            0.25m,
            0.5m,
            0.5m,
            0.8m,
            0m,
            3000m,
            5000m,
            0.001m,
            5m,
            100,
            new DateOnly(2026, 1, 1));

    private static Mock<IRepository<TEntity>> CreateRepository<TEntity>(
        IEnumerable<TEntity> entities)
        where TEntity : class
    {
        var repository = new Mock<IRepository<TEntity>>();
        repository
            .Setup(x => x.GetQueryable(
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<TEntity, object>>[]>()))
            .Returns(entities.AsAsyncQueryable());
        return repository;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
