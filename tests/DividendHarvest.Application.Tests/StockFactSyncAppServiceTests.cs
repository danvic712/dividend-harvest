using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockFactSyncAppServiceTests
{
    [Fact]
    public async Task SyncAsync_reuses_security_context_and_continues_after_a_data_kind_failure()
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
        var securityRepository = RepositoryMock.Create([security]);
        var unitOfWork = new Mock<IUow>();
        unitOfWork
            .Setup(x => x.Get<Security>())
            .Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<DividendEvent>())
            .Returns(RepositoryMock.Create<DividendEvent>([]).Object);
        unitOfWork
            .Setup(x => x.Get<FinancialSnapshot>())
            .Returns(RepositoryMock.Create<FinancialSnapshot>([]).Object);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetMarketDataAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProviderFailureException());
        provider
            .Setup(x => x.GetDividendEventsAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockDividendData>());
        provider
            .Setup(x => x.GetFinancialSnapshotsAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockFinancialData>());
        var service = new StockFactSyncAppService(
            unitOfWork.Object,
            provider.Object);

        var result = await service.SyncAsync(
            AShareReference.Create("000001", "SZSE"),
            CancellationToken.None);

        var failure = Assert.Single(result.Failures);
        Assert.Equal("price", failure.DataKind);
        Assert.Equal("stock_market_data_unavailable", failure.ErrorCode);
        Assert.Equal("000001", failure.Parameters["securityCode"]);
        Assert.Empty(result.DividendEvents);
        Assert.Empty(result.FinancialSnapshots);
        securityRepository.Verify(x => x.SingleOrDefaultAsync(
            It.IsAny<Expression<Func<Security, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()), Times.Once);
        provider.Verify(x => x.GetMarketDataAsync(
            It.IsAny<AShareReference>(),
            It.IsAny<CancellationToken>()), Times.Once);
        provider.Verify(x => x.GetDividendEventsAsync(
            It.IsAny<AShareReference>(),
            It.IsAny<CancellationToken>()), Times.Once);
        provider.Verify(x => x.GetFinancialSnapshotsAsync(
            It.IsAny<AShareReference>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class ProviderFailureException()
        : Exception,
            IStockDataProviderFailure;
}
