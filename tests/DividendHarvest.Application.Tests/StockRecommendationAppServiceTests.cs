using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.DividendStrategy;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockRecommendationAppServiceTests
{
    [Fact]
    public async Task GetAsync_applies_single_stock_allocation_after_analysis()
    {
        var portfolioId = Guid.NewGuid();
        var parameters = CreateParameters(portfolioId);
        var analysis = CreateAnalysis(parameters.Id);
        var watchlist = new Mock<IStockWatchlistAppService>();
        watchlist
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new StockWatchlistItem(
                    analysis.SecurityCode,
                    analysis.ExchangeCode,
                    analysis.SecurityName,
                    "A-share",
                    "CNY",
                    null)
                {
                    SecurityId = analysis.SecurityId
                }]);
        var stockAnalysis = new Mock<IStockAnalysisAppService>();
        stockAnalysis
            .Setup(x => x.GetAsync(
                It.IsAny<GetStockAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        var budget = new Mock<IBudgetAppService>();
        budget
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
            .Returns(RepositoryMock.Create([parameters]).Object);
        var service = new StockRecommendationAppService(
            stockAnalysis.Object,
            watchlist.Object,
            budget.Object,
            new PortfolioAllocationAppService(unitOfWork.Object, TimeProvider.System));

        var result = await service.GetAsync(
            new GetStockAnalysisRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Equal(600, result.SuggestedBuyShares);
        Assert.Equal(2400m, result.SuggestedTradeAmount);
        Assert.Equal(analysis.SecurityId, result.Analysis.SecurityId);
    }

    [Fact]
    public async Task GetAsync_analyzes_the_full_watchlist_before_returning_one_stock()
    {
        var target = CreateStock("000001", "SZSE", "平安银行");
        var other = CreateStock("600001", "SSE", "示例银行");
        var targetAnalysis = CreateAnalysis(
            Guid.NewGuid(),
            securityCode: target.SecurityCode,
            exchangeCode: target.ExchangeCode,
            securityName: target.SecurityName,
            securityId: target.SecurityId);
        var otherAnalysis = CreateAnalysis(
            Guid.NewGuid(),
            securityCode: other.SecurityCode,
            exchangeCode: other.ExchangeCode,
            securityName: other.SecurityName,
            securityId: other.SecurityId);
        var watchlist = new Mock<IStockWatchlistAppService>();
        watchlist
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([target, other]);
        var stockAnalysis = new Mock<IStockAnalysisAppService>();
        stockAnalysis
            .SetupSequence(x => x.GetAsync(
                It.IsAny<GetStockAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAnalysis)
            .ReturnsAsync(otherAnalysis);
        var budget = new Mock<IBudgetAppService>();
        var budgetSummary = new BudgetSummary(
            Guid.NewGuid(),
            "长期股息组合",
            5000m,
            0m,
            5000m,
            1,
            DateTimeOffset.UtcNow);
        budget
            .Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(budgetSummary);
        var allocation = new Mock<IPortfolioAllocationAppService>();
        allocation
            .Setup(x => x.RunAsync(
                It.IsAny<IReadOnlyList<StockWatchlistItem>>(),
                It.IsAny<IReadOnlyList<StockAnalysisResult>>(),
                budgetSummary,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PortfolioRecommendationResult(
                budgetSummary.PortfolioId,
                1000m,
                1000m,
                0m,
                0m,
                [
                    new StockRecommendationResult(targetAnalysis, 0, 0, 0m, 0m),
                    new StockRecommendationResult(otherAnalysis, 0, 0, 0m, 0m)
                ],
                DateTimeOffset.UtcNow));
        var service = new StockRecommendationAppService(
            stockAnalysis.Object,
            watchlist.Object,
            budget.Object,
            allocation.Object);

        var result = await service.GetAsync(
            new GetStockAnalysisRequest(target.SecurityCode, target.ExchangeCode),
            CancellationToken.None);

        Assert.Equal(target.SecurityId, result.Analysis.SecurityId);
        stockAnalysis.Verify(x => x.GetAsync(
            It.IsAny<GetStockAnalysisRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        allocation.Verify(x => x.RunAsync(
            It.Is<IReadOnlyList<StockWatchlistItem>>(items => items.Count == 2),
            It.Is<IReadOnlyList<StockAnalysisResult>>(items =>
                items.Count == 2
                && items[0].SecurityId == target.SecurityId
                && items[1].SecurityId == other.SecurityId),
            budgetSummary,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static StockWatchlistItem CreateStock(
        string securityCode,
        string exchangeCode,
        string securityName)
        => new(
            securityCode,
            exchangeCode,
            securityName,
            "A-share",
            "CNY",
            null)
        {
            SecurityId = Guid.NewGuid()
        };

    private static StockAnalysisResult CreateAnalysis(
        Guid modelParameterSetId,
        string securityCode = "000001",
        string exchangeCode = "SZSE",
        string securityName = "平安银行",
        Guid? securityId = null)
        => new(
            securityCode,
            exchangeCode,
            securityName,
            "available",
            "passed",
            4m,
            0.40m,
            "ttm",
            0.10m,
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
            new DateOnly(2026, 9, 1),
            modelParameterSetId,
            DateTimeOffset.UtcNow,
            "测试分析结果",
            securityId ?? Guid.NewGuid());

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
}
