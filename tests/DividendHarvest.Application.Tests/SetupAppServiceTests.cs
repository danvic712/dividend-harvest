using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Setup;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using PortfolioEntity = DividendHarvest.Domain.Models.Portfolio;
using DividendHarvest.Domain.Securities;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class SetupAppServiceTests
{
    [Fact]
    public async Task GetStatus_returns_missing_requirements_before_initialization()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var service = CreateService(repository);

        var result = await service.GetStatusAsync(CancellationToken.None);

        Assert.False(result.IsComplete);
        Assert.Equal(["portfolio", "stocks"], result.MissingRequirements);
    }

    [Fact]
    public async Task GetStatus_returns_no_missing_requirements_after_initialization()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: true);
        var service = CreateService(repository);

        var result = await service.GetStatusAsync(CancellationToken.None);

        Assert.True(result.IsComplete);
        Assert.Empty(result.MissingRequirements);
    }

    [Fact]
    public async Task InitializeAsync_saves_multiple_stocks_and_optional_initial_holding_atomically()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var securityRepository = new Mock<IRepository<Security>>();
        var positionRepository = new Mock<IRepository<PortfolioPosition>>();
        var unitOfWork = CreateUnitOfWork(repository, securityRepository, positionRepository);
        var scheduler = new Mock<IStockDataSyncScheduler>();
        scheduler.Setup(x => x.TrySchedule()).Returns(true);
        var service = new SetupAppService(unitOfWork.Object, scheduler.Object, CreateRequestValidator());
        var request = new SetupRequest(
            "长期股息组合",
            [
                new SetupStockRequest("000001", "SZSE", new InitialHoldingInput(100, 60, 200, 10.25m)),
                new SetupStockRequest("600036", "SSE", null)
            ]);

        var result = await service.InitializeAsync(request, CancellationToken.None);

        Assert.Equal("长期股息组合", result.PortfolioName);
        Assert.True(result.StockDataSyncScheduled);
        Assert.Equal(2, result.Stocks.Count);
        Assert.Null(result.Stocks[0].SecurityName);
        repository.Verify(x => x.AddAsync(
            It.Is<PortfolioEntity>(portfolio => portfolio.Name == "长期股息组合"),
            It.IsAny<CancellationToken>()), Times.Once);
        securityRepository.Verify(x => x.AddAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        securityRepository.Verify(x => x.AddAsync(
            It.Is<Security>(security =>
                security.SecurityCode == "000001"
                && security.SecurityName == string.Empty
                && security.MarketCode == "A-share"
                && security.CurrencyCode == "CNY"),
            It.IsAny<CancellationToken>()), Times.Once);
        positionRepository.Verify(x => x.AddAsync(
            It.Is<PortfolioPosition>(position => position.HeldShares == 100 && position.CoreShares == 60),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(x => x.TrySchedule(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_rejects_duplicate_stocks_before_scheduling_sync()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var scheduler = new Mock<IStockDataSyncScheduler>();
        var service = CreateService(repository, scheduler);
        var request = new SetupRequest(
            "长期股息组合",
            [
                new SetupStockRequest("000001", "SZSE", null),
                new SetupStockRequest("000001", "SZSE", null)
            ]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.InitializeAsync(request, CancellationToken.None));

        scheduler.Verify(x => x.TrySchedule(), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_does_not_write_when_setup_is_already_complete()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: true);
        var scheduler = new Mock<IStockDataSyncScheduler>();
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(unitOfWork.Object, scheduler.Object, CreateRequestValidator());

        await Assert.ThrowsAsync<ApplicationErrorException>(() => service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None));

        scheduler.Verify(x => x.TrySchedule(), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_succeeds_without_stock_provider_data()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var scheduler = new Mock<IStockDataSyncScheduler>();
        scheduler.Setup(x => x.TrySchedule()).Returns(true);
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(unitOfWork.Object, scheduler.Object, CreateRequestValidator());

        var result = await service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None);

        Assert.True(result.StockDataSyncScheduled);
        Assert.Null(result.Stocks[0].SecurityName);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.AddAsync(It.IsAny<PortfolioEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_succeeds_when_background_sync_cannot_be_scheduled()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var scheduler = new Mock<IStockDataSyncScheduler>();
        scheduler.Setup(x => x.TrySchedule()).Returns(false);
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(unitOfWork.Object, scheduler.Object, CreateRequestValidator());

        var result = await service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None);

        Assert.False(result.StockDataSyncScheduled);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.AddAsync(It.IsAny<PortfolioEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_validates_request_before_checking_setup_status()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var scheduler = new Mock<IStockDataSyncScheduler>();
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(
            unitOfWork.Object,
            scheduler.Object,
            CreateRequestValidator());

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(() => service.InitializeAsync(
            new SetupRequest(" ", []),
            CancellationToken.None));

        Assert.Contains(
            "投资组合名称必须为 1 到 100 个字符。",
            exception.Parameters["message"]?.ToString());
        unitOfWork.Verify(x => x.Get<PortfolioEntity>(), Times.Never);
        scheduler.Verify(x => x.TrySchedule(), Times.Never);
    }

    private static SetupAppService CreateService(
        Mock<IRepository<PortfolioEntity>> repository,
        Mock<IStockDataSyncScheduler>? scheduler = null)
    {
        var unitOfWork = CreateUnitOfWork(repository);
        return new SetupAppService(
            unitOfWork.Object,
            (scheduler ?? new Mock<IStockDataSyncScheduler>()).Object,
            CreateRequestValidator());
    }

    private static SetupRequestValidator CreateRequestValidator()
        => new(new SetupStockRequestValidator(new InitialHoldingInputValidator()));

    private static Mock<IRepository<PortfolioEntity>> CreatePortfolioRepository(bool hasPortfolio)
        => RepositoryMock.Create(new[]
            {
                hasPortfolio
                    ? new PortfolioEntity { Id = Guid.NewGuid(), Name = "长期股息组合" }
                    : null
            }
            .Where(entity => entity is not null)
            .Cast<PortfolioEntity>());

    private static Mock<IUow> CreateUnitOfWork(
        Mock<IRepository<PortfolioEntity>> portfolioRepository,
        Mock<IRepository<Security>>? securityRepository = null,
        Mock<IRepository<PortfolioPosition>>? positionRepository = null)
    {
        securityRepository ??= new Mock<IRepository<Security>>();
        positionRepository ??= new Mock<IRepository<PortfolioPosition>>();
        var unitOfWork = new Mock<IUow>();
        unitOfWork
            .Setup(x => x.Get<PortfolioEntity>())
            .Returns(portfolioRepository.Object);
        unitOfWork
            .Setup(x => x.Get<Security>())
            .Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<PortfolioPosition>())
            .Returns(positionRepository.Object);
        unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        return unitOfWork;
    }
}
