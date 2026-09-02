using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Analysis;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockAnalysisAppServiceTests
{
    [Fact]
    public async Task GetAsync_calculates_ttm_dividend_yield_and_price_zone()
    {
        var security = CreateSecurity();
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = "长期股息组合"
        };
        var parameters = CreateParameters(portfolio.Id, security.Id);
        var priceObservation = PriceObservation.Create(
            security.Id,
            new DateOnly(2026, 9, 1),
            4m,
            new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
            "FTShare",
            "price-1",
            "valid");
        var position = new PortfolioPosition
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            HeldShares = 100,
            CoreShares = 60,
            TargetShares = 200,
            AverageCostPerShare = 3.50m
        };
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            CreateRepository([parameters]),
            CreateRepository([priceObservation]),
            CreateRepository([
                CreateDividendEvent(security.Id, 0.20m, new DateOnly(2026, 8, 1), "current"),
                CreateDividendEvent(security.Id, 0.12m, new DateOnly(2026, 3, 1), "previous"),
                CreateDividendEvent(
                    security.Id,
                    0.90m,
                    new DateOnly(2026, 7, 1),
                    "proposed",
                    "proposed",
                    false),
                CreateDividendEvent(
                    security.Id,
                    0.50m,
                    new DateOnly(2026, 6, 1),
                    "special",
                    "implemented",
                    true),
                CreateDividendEvent(
                    security.Id,
                    1.00m,
                    new DateOnly(2025, 8, 1),
                    "outside-window")
            ]),
            CreateRepository<FinancialSnapshot>([]),
            CreateRepository([position]));
        var service = CreateService(unitOfWork.Object);

        var result = await service.GetAsync(
            new GetStockAnalysisRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Equal("000001", result.SecurityCode);
        Assert.Equal("cautious", result.ModelStatusCode);
        Assert.Equal("cautious", result.DividendReliabilityCode);
        Assert.Equal(4m, result.ClosePrice);
        Assert.Equal(0.32m, result.ModelDividendPerShare);
        Assert.Equal("ttm", result.DividendModeCode);
        Assert.Equal(0.08m, result.DividendYield);
        Assert.Equal("strong_buy", result.PriceZoneCode);
        Assert.Equal("no_action", result.RecommendationCode);
        Assert.Equal(100, result.HeldShares);
        Assert.Equal(60, result.CoreShares);
        Assert.Equal(40, result.SatelliteShares);
        Assert.Equal(0, result.SuggestedBuyShares);
        Assert.Equal(new DateOnly(2026, 9, 1), result.DataAsOfDate);
        Assert.Equal(parameters.Id, result.ModelParameterSetId);
    }

    [Fact]
    public async Task GetAsync_returns_unavailable_when_ttm_dividend_is_missing()
    {
        var security = CreateSecurity();
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = "长期股息组合"
        };
        var parameters = CreateParameters(portfolio.Id, security.Id);
        var priceObservation = PriceObservation.Create(
            security.Id,
            new DateOnly(2026, 9, 1),
            4m,
            new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
            "FTShare",
            "price-1",
            "valid");
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            CreateRepository([parameters]),
            CreateRepository([priceObservation]),
            CreateRepository<DividendEvent>([]),
            CreateRepository<FinancialSnapshot>([]),
            CreateRepository<PortfolioPosition>([]));
        var service = CreateService(unitOfWork.Object);

        var result = await service.GetAsync(
            new GetStockAnalysisRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Equal("unavailable", result.ModelStatusCode);
        Assert.Equal("unavailable", result.DividendReliabilityCode);
        Assert.Null(result.ModelDividendPerShare);
        Assert.Null(result.PriceZoneCode);
        Assert.Equal("no_action", result.RecommendationCode);
        Assert.Equal(0, result.SuggestedBuyShares);
        Assert.Equal(0, result.SuggestedSellShares);
    }

    [Fact]
    public async Task GetAsync_exposes_recommendation_when_dividend_reliability_passes()
    {
        var security = CreateSecurity();
        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            Name = "长期股息组合"
        };
        var parameters = CreateParameters(portfolio.Id, security.Id);
        var priceObservation = PriceObservation.Create(
            security.Id,
            new DateOnly(2026, 9, 1),
            4m,
            new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
            "FTShare",
            "price-passed",
            "valid");
        var dividendEvents = Enumerable.Range(2021, 5)
            .Select(year => CreateDividendEvent(
                security.Id,
                0.20m,
                new DateOnly(year, 6, 1),
                $"dividend-{year}"))
            .Append(CreateDividendEvent(
                security.Id,
                0.20m,
                new DateOnly(2026, 6, 1),
                "dividend-2026"))
            .Append(CreateDividendEvent(
                security.Id,
                0.20m,
                new DateOnly(2025, 10, 1),
                "dividend-2025-ttm"))
            .ToArray();
        var financialSnapshot = FinancialSnapshot.Create(
            security.Id,
            new DateOnly(2025, 12, 31),
            new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 9, 8, 0, 0, TimeSpan.Zero),
            0.80m,
            0.45m,
            0.40m,
            0.90m,
            0.12m,
            "FTShare",
            "financial-2025",
            "valid");
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            CreateRepository([parameters]),
            CreateRepository([priceObservation]),
            CreateRepository(dividendEvents),
            CreateRepository([financialSnapshot]),
            CreateRepository<PortfolioPosition>([]),
            CreateRepository([
                CashLedgerEntry.Create(
                    portfolio.Id,
                    null,
                    new DateOnly(2026, 8, 1),
                    "budget_deposit",
                    "inflow",
                    5000m,
                    "deposit-1")
            ]));
        var service = CreateService(unitOfWork.Object);

        var result = await service.GetAsync(
            new GetStockAnalysisRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Equal("available", result.ModelStatusCode);
        Assert.Equal("passed", result.DividendReliabilityCode);
        Assert.Equal("strong_buy", result.PriceZoneCode);
        Assert.Equal("strong_buy", result.RecommendationCode);
        Assert.Equal(200, result.SuggestedBuyShares);
        Assert.Equal(800m, result.SuggestedTradeAmount);
    }

    [Fact]
    public async Task GetAsync_validates_stock_reference_before_accessing_the_database()
    {
        var unitOfWork = new Mock<IUow>();
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<StockAnalysisValidationException>(() =>
            service.GetAsync(
                new GetStockAnalysisRequest("123", "NYSE"),
                CancellationToken.None));

        unitOfWork.Verify(x => x.Get<Security>(), Times.Never);
    }

    private static StockAnalysisAppService CreateService(IUow unitOfWork)
        => new(
            unitOfWork,
            new GetStockAnalysisRequestValidator(),
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

    private static Security CreateSecurity()
        => new()
        {
            Id = Guid.NewGuid(),
            SecurityCode = "000001",
            ExchangeCode = "SZSE",
            SecurityName = "平安银行",
            MarketCode = "A-share",
            CurrencyCode = "CNY"
        };

    private static ModelParameterSet CreateParameters(Guid portfolioId, Guid securityId)
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
            0.2m,
            0.4m,
            0.1m,
            1000m,
            5000m,
            0.001m,
            5m,
            100,
            new DateOnly(2026, 1, 1));

    private static DividendEvent CreateDividendEvent(
        Guid securityId,
        decimal dividendPerShare,
        DateOnly exDividendDate,
        string sourceRecordId,
        string dividendStatusCode = "implemented",
        bool isSpecialDividend = false)
        => DividendEvent.Create(
            securityId,
            dividendPerShare,
            isSpecialDividend ? "special_cash" : "regular_cash",
            dividendStatusCode,
            new DateOnly(exDividendDate.Year, 5, 1),
            exDividendDate,
            exDividendDate.AddDays(1),
            isSpecialDividend,
            new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            "FTShare",
            sourceRecordId,
            "valid");

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

    private static Mock<IUow> CreateUnitOfWork(
        Mock<IRepository<Security>> securityRepository,
        Mock<IRepository<ModelParameterSet>> parameterRepository,
        Mock<IRepository<PriceObservation>> priceRepository,
        Mock<IRepository<DividendEvent>> dividendRepository,
        Mock<IRepository<FinancialSnapshot>> financialRepository,
        Mock<IRepository<PortfolioPosition>> positionRepository,
        Mock<IRepository<CashLedgerEntry>>? cashLedgerRepository = null)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<ModelParameterSet>())
            .Returns(parameterRepository.Object);
        unitOfWork
            .Setup(x => x.Get<PriceObservation>())
            .Returns(priceRepository.Object);
        unitOfWork
            .Setup(x => x.Get<DividendEvent>())
            .Returns(dividendRepository.Object);
        unitOfWork
            .Setup(x => x.Get<FinancialSnapshot>())
            .Returns(financialRepository.Object);
        unitOfWork
            .Setup(x => x.Get<PortfolioPosition>())
            .Returns(positionRepository.Object);
        unitOfWork
            .Setup(x => x.Get<CashLedgerEntry>())
            .Returns((cashLedgerRepository ?? CreateRepository<CashLedgerEntry>([])).Object);
        return unitOfWork;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
