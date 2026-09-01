using DividendHarvest.Application.Ports;
using DividendHarvest.Application.Setup;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class SetupAppServiceTests
{
    [Fact]
    public async Task GetStatus_returns_missing_requirements_before_initialization()
    {
        var repository = new Mock<ISetupRepository>();
        repository
            .Setup(x => x.IsSetupCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(repository);

        var result = await service.GetStatusAsync(CancellationToken.None);

        Assert.False(result.IsComplete);
        Assert.Equal(["portfolio", "stocks"], result.MissingRequirements);
    }

    [Fact]
    public async Task GetStatus_returns_no_missing_requirements_after_initialization()
    {
        var repository = new Mock<ISetupRepository>();
        repository
            .Setup(x => x.IsSetupCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(repository);

        var result = await service.GetStatusAsync(CancellationToken.None);

        Assert.True(result.IsComplete);
        Assert.Empty(result.MissingRequirements);
    }

    [Fact]
    public async Task InitializeAsync_saves_multiple_stocks_and_optional_initial_holding_atomically()
    {
        var repository = new Mock<ISetupRepository>();
        repository
            .Setup(x => x.IsSetupCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetAsync(It.Is<DividendHarvest.Domain.Securities.AShareReference>(r => r.SecurityCode == "000001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockData("000001", "SZSE", "平安银行", "A-share", "CNY"));
        provider
            .Setup(x => x.GetAsync(It.Is<DividendHarvest.Domain.Securities.AShareReference>(r => r.SecurityCode == "600036"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockData("600036", "SSE", "招商银行", "A-share", "CNY"));
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken));
        var service = new SetupAppService(repository.Object, provider.Object, unitOfWork.Object);
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
        repository.Verify(x => x.AddPortfolioAsync(It.Is<PortfolioRecord>(p => p.Name == "长期股息组合"), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.AddSecurityAsync(It.IsAny<SecurityRecord>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        repository.Verify(x => x.AddPositionAsync(It.Is<PositionRecord>(p => p.HeldShares == 100 && p.CoreShares == 60), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_rejects_duplicate_stocks_before_fetching_data()
    {
        var repository = new Mock<ISetupRepository>();
        repository
            .Setup(x => x.IsSetupCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var provider = new Mock<IStockDataProvider>();
        var service = CreateService(repository, provider);
        var request = new SetupRequest(
            "长期股息组合",
            [
                new SetupStockRequest("000001", "SZSE", null),
                new SetupStockRequest("000001", "SZSE", null)
            ]);

        await Assert.ThrowsAsync<SetupValidationException>(() => service.InitializeAsync(request, CancellationToken.None));

        provider.Verify(x => x.GetAsync(It.IsAny<DividendHarvest.Domain.Securities.AShareReference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_does_not_write_when_setup_is_already_complete()
    {
        var repository = new Mock<ISetupRepository>();
        repository
            .Setup(x => x.IsSetupCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var provider = new Mock<IStockDataProvider>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new SetupAppService(repository.Object, provider.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<SetupAlreadyCompletedException>(() => service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None));

        provider.Verify(x => x.GetAsync(It.IsAny<DividendHarvest.Domain.Securities.AShareReference>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_does_not_write_when_stock_data_is_unavailable()
    {
        var repository = new Mock<ISetupRepository>();
        repository
            .Setup(x => x.IsSetupCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetAsync(It.IsAny<DividendHarvest.Domain.Securities.AShareReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockData?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new SetupAppService(repository.Object, provider.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<StockDataUnavailableException>(() => service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None));

        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddPortfolioAsync(It.IsAny<PortfolioRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_translates_provider_failure_without_writing()
    {
        var repository = new Mock<ISetupRepository>();
        repository
            .Setup(x => x.IsSetupCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetAsync(It.IsAny<DividendHarvest.Domain.Securities.AShareReference>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StockDataProviderUnavailableException(
                "FTShare MCP 股票资料暂时不可用。",
                new TimeoutException("FTShare MCP 请求超时。")));
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = new SetupAppService(repository.Object, provider.Object, unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<StockDataUnavailableException>(() => service.InitializeAsync(
            new SetupRequest("长期股息组合", [new SetupStockRequest("000001", "SZSE", null)]),
            CancellationToken.None));

        Assert.IsType<StockDataProviderUnavailableException>(exception.InnerException);
        unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddPortfolioAsync(It.IsAny<PortfolioRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SetupAppService CreateService(
        Mock<ISetupRepository> repository,
        Mock<IStockDataProvider>? provider = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken));
        return new SetupAppService(repository.Object, (provider ?? new Mock<IStockDataProvider>()).Object, unitOfWork.Object);
    }
}
