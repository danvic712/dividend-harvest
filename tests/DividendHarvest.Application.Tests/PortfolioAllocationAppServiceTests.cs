using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.DividendStrategy;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class PortfolioAllocationAppServiceTests
{
    [Fact]
    public async Task RunAsync_allocates_budget_to_strong_buy_before_accumulate()
    {
        var portfolioId = Guid.NewGuid();
        var firstSecurityId = Guid.NewGuid();
        var secondSecurityId = Guid.NewGuid();
        var firstParameter = CreateParameters(portfolioId, firstSecurityId);
        var secondParameter = CreateParameters(portfolioId, secondSecurityId);
        var stocks = new[]
        {
            CreateStock("000001", "SZSE", "平安银行", firstSecurityId),
            CreateStock("600001", "SSE", "示例银行", secondSecurityId)
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
                securityId: firstSecurityId),
            CreateAnalysis(
                "600001",
                "SSE",
                "示例银行",
                "accumulate",
                secondParameter.Id,
                5m,
                securityId: secondSecurityId)
        };
        var unitOfWork = CreateUnitOfWork([firstParameter, secondParameter]);
        var service = new PortfolioAllocationAppService(
            unitOfWork.Object,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

        var result = await service.RunAsync(
            stocks,
            analyses,
            CreateBudgetSummary(portfolioId),
            CancellationToken.None);

        Assert.Equal(5000m, result.StartingAvailableBudgetAmount);
        Assert.Equal(600, result.Stocks[0].SuggestedBuyShares);
        Assert.Equal(100, result.Stocks[1].SuggestedBuyShares);
        Assert.Equal(2900m, result.TotalSuggestedTradeAmount);
        Assert.Equal(10m, result.EstimatedTransactionFeeAmount);
        Assert.Equal(2090m, result.RemainingAvailableBudgetAmount);
    }

    [Fact]
    public async Task RunAsync_keeps_cautious_stock_without_a_trade_quantity()
    {
        var portfolioId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var parameter = CreateParameters(portfolioId, securityId);
        var stock = CreateStock("000001", "SZSE", "平安银行", securityId);
        var analysis = CreateAnalysis(
            "000001",
            "SZSE",
            "平安银行",
            "strong_buy",
            parameter.Id,
            4m,
            "cautious",
            "cautious",
            "no_action",
            securityId: securityId);
        var service = new PortfolioAllocationAppService(
            CreateUnitOfWork([parameter]).Object,
            TimeProvider.System);

        var result = await service.RunAsync(
            [stock],
            [analysis],
            CreateBudgetSummary(portfolioId),
            CancellationToken.None);

        Assert.Equal(0, result.Stocks[0].SuggestedBuyShares);
        Assert.Equal(0m, result.TotalSuggestedTradeAmount);
    }

    [Fact]
    public async Task RunAsync_does_not_allocate_buy_budget_when_a_held_position_lacks_a_price()
    {
        var portfolioId = Guid.NewGuid();
        var firstSecurityId = Guid.NewGuid();
        var secondSecurityId = Guid.NewGuid();
        var firstParameter = CreateParameters(portfolioId, firstSecurityId);
        var secondParameter = CreateParameters(portfolioId, secondSecurityId);
        var stocks = new[]
        {
            CreateStock("000001", "SZSE", "平安银行", firstSecurityId),
            CreateStock("600001", "SSE", "示例银行", secondSecurityId)
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
                heldShares: 100,
                securityId: firstSecurityId),
            CreateAnalysis(
                "600001",
                "SSE",
                "示例银行",
                "hold",
                secondParameter.Id,
                null,
                recommendationCode: "hold",
                heldShares: 50,
                securityId: secondSecurityId)
        };
        var service = new PortfolioAllocationAppService(
            CreateUnitOfWork([firstParameter, secondParameter]).Object,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

        var result = await service.RunAsync(
            stocks,
            analyses,
            CreateBudgetSummary(portfolioId),
            CancellationToken.None);

        Assert.Equal(0m, result.StartingAvailableBudgetAmount);
        Assert.Equal(0, result.Stocks[0].SuggestedBuyShares);
        Assert.Equal(0m, result.TotalSuggestedTradeAmount);
    }

    [Fact]
    public async Task RunAsync_rejects_analysis_with_a_mismatched_security_identity()
    {
        var portfolioId = Guid.NewGuid();
        var stock = CreateStock("000001", "SZSE", "平安银行", Guid.NewGuid());
        var analysis = CreateAnalysis(
            "000001",
            "SZSE",
            "平安银行",
            "strong_buy",
            Guid.NewGuid(),
            4m,
            securityId: Guid.NewGuid());
        var unitOfWork = new Mock<IUow>();
        var service = new PortfolioAllocationAppService(
            unitOfWork.Object,
            TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RunAsync(
            [stock],
            [analysis],
            CreateBudgetSummary(portfolioId),
            CancellationToken.None));

        unitOfWork.Verify(x => x.Get<ModelParameterSet>(), Times.Never);
    }

    private static StockWatchlistItem CreateStock(
        string securityCode,
        string exchangeCode,
        string securityName,
        Guid securityId)
        => new(
            securityCode,
            exchangeCode,
            securityName,
            "A-share",
            "CNY",
            null)
        {
            SecurityId = securityId
        };

    private static BudgetSummary CreateBudgetSummary(Guid portfolioId)
        => new(
            portfolioId,
            "长期股息组合",
            5000m,
            0m,
            5000m,
            1,
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));

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
        int heldShares = 0,
        Guid? securityId = null)
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
            new DateOnly(2026, 9, 1),
            modelParameterSetId,
            new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
            "测试分析结果",
            securityId ?? Guid.NewGuid());

    private static ModelParameterSet CreateParameters(
        Guid portfolioId,
        Guid securityId)
        => ModelParameterSet.Create(
            portfolioId,
            securityId,
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

    private static Mock<IUow> CreateUnitOfWork(
        IEnumerable<ModelParameterSet> parameters)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork
            .Setup(x => x.Get<ModelParameterSet>())
            .Returns(RepositoryMock.Create(parameters).Object);
        return unitOfWork;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
