using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.DividendStrategy;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class PortfolioRecommendationAppServiceTests
{
    [Fact]
    public async Task GetAsync_delegates_aligned_analysis_and_budget_to_allocation()
    {
        var securityId = Guid.NewGuid();
        var stock = new StockWatchlistItem(
            "000001",
            "SZSE",
            "平安银行",
            "A-share",
            "CNY",
            null)
        {
            SecurityId = securityId
        };
        var analysis = CreateAnalysis(securityId);
        var budgetSummary = new BudgetSummary(
            Guid.NewGuid(),
            "长期股息组合",
            5000m,
            0m,
            5000m,
            1,
            DateTimeOffset.UtcNow);
        var watchlist = new Mock<IStockWatchlistAppService>();
        watchlist
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([stock]);
        var stockAnalysis = new Mock<IStockAnalysisAppService>();
        stockAnalysis
            .Setup(x => x.GetAsync(
                It.IsAny<GetStockAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);
        var budget = new Mock<IBudgetAppService>();
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
                100m,
                900m,
                5m,
                [new StockRecommendationResult(analysis, 0, 0, 0m, 0m)],
                DateTimeOffset.UtcNow));
        var service = new PortfolioRecommendationAppService(
            watchlist.Object,
            stockAnalysis.Object,
            budget.Object,
            allocation.Object);

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal(budgetSummary.PortfolioId, result.PortfolioId);
        allocation.Verify(x => x.RunAsync(
            It.Is<IReadOnlyList<StockWatchlistItem>>(items =>
                items.Count == 1 && items[0].SecurityId == securityId),
            It.Is<IReadOnlyList<StockAnalysisResult>>(items =>
                items.Count == 1 && items[0].SecurityId == securityId),
            budgetSummary,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static StockAnalysisResult CreateAnalysis(Guid securityId)
        => new(
            "000001",
            "SZSE",
            "平安银行",
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
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "测试分析结果",
            securityId);
}
