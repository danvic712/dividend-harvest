using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.DividendStrategy;
using DividendHarvest.Application.Validators;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using PortfolioEntity = DividendHarvest.Domain.Models.Portfolio;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockModelParameterAppServiceTests
{
    [Fact]
    public async Task SaveAsync_persists_parameters_for_a_configured_stock()
    {
        var security = new Security
        {
            Id = Guid.NewGuid(),
            SecurityCode = "000001",
            ExchangeCode = "SZSE",
            SecurityName = "平安银行",
            MarketCode = "A-share",
            CurrencyCode = "CNY"
        };
        var portfolio = new PortfolioEntity
        {
            Id = Guid.NewGuid(),
            Name = "长期股息组合"
        };
        var parameterRepository = CreateRepository<ModelParameterSet>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            CreateRepository([portfolio]),
            parameterRepository);
        var service = CreateService(unitOfWork.Object);
        var request = CreateRequest();

        var result = await service.SaveAsync(request, CancellationToken.None);

        Assert.Equal("000001", result.SecurityCode);
        Assert.Equal("SZSE", result.ExchangeCode);
        Assert.Equal("v1", result.ModelVersion);
        Assert.Equal(new DateOnly(2026, 9, 2), result.EffectiveFromDate);
        parameterRepository.Verify(x => x.AddAsync(
            It.Is<ModelParameterSet>(parameters =>
                parameters.SecurityId == security.Id
                && parameters.PortfolioId == portfolio.Id
                && parameters.StrongBuyYieldThreshold == 0.08m),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_rejects_a_stock_that_is_not_in_the_watchlist()
    {
        var unitOfWork = CreateUnitOfWork(
            CreateRepository<Security>([]),
            CreateRepository([new PortfolioEntity { Id = Guid.NewGuid(), Name = "长期股息组合" }]),
            CreateRepository<ModelParameterSet>([]));
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<StockNotConfiguredException>(() => service.SaveAsync(
            CreateRequest(),
            CancellationToken.None));

        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_validates_input_before_accessing_the_database()
    {
        var unitOfWork = new Mock<IUow>();
        var service = CreateService(unitOfWork.Object);
        var invalidRequest = CreateRequest() with
        {
            StrongBuyBudgetRatio = 1.01m
        };

        await Assert.ThrowsAsync<ModelParameterValidationException>(() =>
            service.SaveAsync(invalidRequest, CancellationToken.None));

        unitOfWork.Verify(x => x.Get<Security>(), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_rejects_a_duplicate_effective_date_without_writing()
    {
        var security = CreateSecurity();
        var portfolio = CreatePortfolio();
        var parameterRepository = CreateRepository([
            CreateParameters(portfolio.Id, security.Id, new DateOnly(2026, 9, 2), "v1")
        ]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            CreateRepository([portfolio]),
            parameterRepository);
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<ModelParameterVersionAlreadyExistsException>(() =>
            service.SaveAsync(CreateRequest(), CancellationToken.None));

        parameterRepository.Verify(x => x.AddAsync(
            It.IsAny<ModelParameterSet>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_returns_the_latest_effective_version_and_ignores_future_versions()
    {
        var security = CreateSecurity();
        var portfolio = CreatePortfolio();
        var parameterRepository = CreateRepository([
            CreateParameters(portfolio.Id, security.Id, new DateOnly(2026, 8, 1), "v1"),
            CreateParameters(portfolio.Id, security.Id, new DateOnly(2026, 9, 1), "v2"),
            CreateParameters(portfolio.Id, security.Id, new DateOnly(2026, 9, 3), "v3")
        ]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            CreateRepository([portfolio]),
            parameterRepository);
        var service = CreateService(
            unitOfWork.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

        var result = await service.GetAsync(
            new GetStockModelParametersRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("v2", result!.ModelVersion);
        Assert.Equal(new DateOnly(2026, 9, 1), result.EffectiveFromDate);
        Assert.Equal(0.08m, result.StrongBuyYieldThreshold);
    }

    private static StockModelParameterAppService CreateService(
        IUow unitOfWork,
        TimeProvider? timeProvider = null)
        => new(
            unitOfWork,
            new SaveStockModelParametersRequestValidator(),
            new GetStockModelParametersRequestValidator(),
            timeProvider ?? TimeProvider.System);

    private static SaveStockModelParametersRequest CreateRequest()
        => new(
            "000001",
            "SZSE",
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
            new DateOnly(2026, 9, 2));

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

    private static PortfolioEntity CreatePortfolio()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "长期股息组合"
        };

    private static ModelParameterSet CreateParameters(
        Guid portfolioId,
        Guid securityId,
        DateOnly effectiveFromDate,
        string modelVersion)
        => ModelParameterSet.Create(
            portfolioId,
            securityId,
            modelVersion,
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
            effectiveFromDate);

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
        Mock<IRepository<PortfolioEntity>> portfolioRepository,
        Mock<IRepository<ModelParameterSet>> parameterRepository)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        unitOfWork.Setup(x => x.Get<PortfolioEntity>()).Returns(portfolioRepository.Object);
        unitOfWork.Setup(x => x.Get<ModelParameterSet>()).Returns(parameterRepository.Object);
        unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
