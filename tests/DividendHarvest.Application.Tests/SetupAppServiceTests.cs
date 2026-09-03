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
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetAsync(It.Is<AShareReference>(r => r.SecurityCode == "000001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockData("000001", "SZSE", "平安银行", "A-share", "CNY"));
        provider
            .Setup(x => x.GetAsync(It.Is<AShareReference>(r => r.SecurityCode == "600036"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockData("600036", "SSE", "招商银行", "A-share", "CNY"));
        var securityRepository = new Mock<IRepository<Security>>();
        var positionRepository = new Mock<IRepository<PortfolioPosition>>();
        var unitOfWork = CreateUnitOfWork(repository, securityRepository, positionRepository);
        var service = new SetupAppService(unitOfWork.Object, provider.Object, CreateRequestValidator());
        var request = new SetupRequest(
            "长期股息组合",
            [
                new SetupStockRequest("000001", "SZSE", new InitialHoldingInput(100, 60, 200, 10.25m)),
                new SetupStockRequest("600036", "SSE", null)
            ]);

        var result = await service.InitializeAsync(request, CancellationToken.None);

        Assert.Equal("长期股息组合", result.PortfolioName);
        Assert.Equal(2, result.Stocks.Count);
        Assert.Equal("平安银行", result.Stocks[0].SecurityName);
        repository.Verify(x => x.AddAsync(
            It.Is<PortfolioEntity>(portfolio => portfolio.Name == "长期股息组合"),
            It.IsAny<CancellationToken>()), Times.Once);
        securityRepository.Verify(x => x.AddAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        positionRepository.Verify(x => x.AddAsync(
            It.Is<PortfolioPosition>(position => position.HeldShares == 100 && position.CoreShares == 60),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_rejects_duplicate_stocks_before_fetching_data()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var provider = new Mock<IStockDataProvider>();
        var service = CreateService(repository, provider);
        var request = new SetupRequest(
            "长期股息组合",
            [
                new SetupStockRequest("000001", "SZSE", null),
                new SetupStockRequest("000001", "SZSE", null)
            ]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => service.InitializeAsync(request, CancellationToken.None));

        provider.Verify(x => x.GetAsync(It.IsAny<AShareReference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_does_not_write_when_setup_is_already_complete()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: true);
        var provider = new Mock<IStockDataProvider>();
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(unitOfWork.Object, provider.Object, CreateRequestValidator());

        await Assert.ThrowsAsync<ApplicationErrorException>(() => service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None));

        provider.Verify(x => x.GetAsync(It.IsAny<AShareReference>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_does_not_write_when_stock_data_is_unavailable()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetAsync(It.IsAny<AShareReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockData?)null);
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(unitOfWork.Object, provider.Object, CreateRequestValidator());

        await Assert.ThrowsAsync<ApplicationErrorException>(() => service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None));

        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.IsAny<PortfolioEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_translates_provider_failure_without_writing()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetAsync(It.IsAny<AShareReference>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TestStockDataProviderFailureException(
                new TimeoutException("FTShare MCP 请求超时。")));
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(unitOfWork.Object, provider.Object, CreateRequestValidator());

        var exception = await Assert.ThrowsAsync<ApplicationErrorException>(() => service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None));

        Assert.Equal(ApplicationErrorCodes.StockDataUnavailable, exception.ErrorCode);
        Assert.IsAssignableFrom<IStockDataProviderFailure>(exception.InnerException);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.IsAny<PortfolioEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_validates_request_before_checking_setup_status()
    {
        var repository = CreatePortfolioRepository(hasPortfolio: false);
        var provider = new Mock<IStockDataProvider>();
        var unitOfWork = CreateUnitOfWork(repository);
        var service = new SetupAppService(
            unitOfWork.Object,
            provider.Object,
            CreateRequestValidator());

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(() => service.InitializeAsync(
            new SetupRequest(" ", []),
            CancellationToken.None));

        Assert.Contains(
            "投资组合名称必须为 1 到 100 个字符。",
            exception.Parameters["message"]?.ToString());
        unitOfWork.Verify(x => x.Get<PortfolioEntity>(), Times.Never);
        provider.Verify(x => x.GetAsync(It.IsAny<AShareReference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SetupAppService CreateService(
        Mock<IRepository<PortfolioEntity>> repository,
        Mock<IStockDataProvider>? provider = null)
    {
        var unitOfWork = CreateUnitOfWork(repository);
        return new SetupAppService(
            unitOfWork.Object,
            (provider ?? new Mock<IStockDataProvider>()).Object,
            CreateRequestValidator());
    }

    private static SetupRequestValidator CreateRequestValidator()
        => new(new SetupStockRequestValidator(new InitialHoldingInputValidator()));

    private static Mock<IRepository<PortfolioEntity>> CreatePortfolioRepository(bool hasPortfolio)
    {
        var repository = new Mock<IRepository<PortfolioEntity>>();
        repository
            .Setup(x => x.GetQueryable(
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<PortfolioEntity, object>>[]>()))
            .Returns(new[]
            {
                hasPortfolio
                    ? new PortfolioEntity { Id = Guid.NewGuid(), Name = "长期股息组合" }
                    : null
            }
            .Where(entity => entity is not null)
            .Cast<PortfolioEntity>()
            .AsAsyncQueryable());
        return repository;
    }

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

    private sealed class TestStockDataProviderFailureException(Exception innerException)
        : Exception(null, innerException), IStockDataProviderFailure;
}
