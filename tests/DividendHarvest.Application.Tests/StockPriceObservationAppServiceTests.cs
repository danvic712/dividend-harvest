using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.PriceObservations;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockPriceObservationAppServiceTests
{
    [Fact]
    public async Task SyncAsync_saves_a_normalized_market_snapshot()
    {
        var security = CreateSecurity();
        var observationRepository = CreateRepository<PriceObservation>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            observationRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetMarketDataAsync(
                It.Is<AShareReference>(reference =>
                    reference.SecurityCode == "000001"
                    && reference.ExchangeCode == "SZSE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockMarketData(
                "000001",
                "SZSE",
                10.25m,
                new DateOnly(2026, 9, 1),
                new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
                "FTShare",
                "record-1",
                "valid"));
        var service = CreateService(unitOfWork.Object, provider.Object);

        var result = await service.SyncAsync(
            new SyncStockPriceRequest(" 000001 ", "szse"),
            CancellationToken.None);

        Assert.Equal("000001", result.SecurityCode);
        Assert.Equal("SZSE", result.ExchangeCode);
        Assert.Equal(10.25m, result.ClosePrice);
        Assert.Equal("valid", result.DataQualityCode);
        observationRepository.Verify(x => x.AddAsync(
            It.Is<PriceObservation>(observation =>
                observation.SecurityId == security.Id
                && observation.TradingDate == new DateOnly(2026, 9, 1)
                && observation.ClosePrice == 10.25m),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_returns_existing_observation_without_a_second_write()
    {
        var security = CreateSecurity();
        var existing = PriceObservation.Create(
            security.Id,
            new DateOnly(2026, 9, 1),
            10m,
            new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
            "FTShare",
            "record-1",
            "valid");
        var observationRepository = CreateRepository([existing]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            observationRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetMarketDataAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockMarketData(
                "000001",
                "SZSE",
                10.25m,
                new DateOnly(2026, 9, 1),
                new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.Zero),
                "FTShare",
                "record-1",
                "valid"));
        var service = CreateService(unitOfWork.Object, provider.Object);

        var result = await service.SyncAsync(
            new SyncStockPriceRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Equal(existing.Id, result.PriceObservationId);
        Assert.Equal(10m, result.ClosePrice);
        observationRepository.Verify(x => x.AddAsync(
            It.IsAny<PriceObservation>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_rejects_unavailable_market_data_without_writing()
    {
        var security = CreateSecurity();
        var observationRepository = CreateRepository<PriceObservation>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            observationRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetMarketDataAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockMarketData?)null);
        var service = CreateService(unitOfWork.Object, provider.Object);

        await Assert.ThrowsAsync<StockMarketDataUnavailableException>(() =>
            service.SyncAsync(
                new SyncStockPriceRequest("000001", "SZSE"),
                CancellationToken.None));

        observationRepository.Verify(x => x.AddAsync(
            It.IsAny<PriceObservation>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_validates_request_before_accessing_the_database()
    {
        var unitOfWork = new Mock<IUow>();
        var provider = new Mock<IStockDataProvider>();
        var service = CreateService(unitOfWork.Object, provider.Object);

        await Assert.ThrowsAsync<StockDataSyncValidationException>(() =>
            service.SyncAsync(
                new SyncStockPriceRequest("123", "NYSE"),
                CancellationToken.None));

        unitOfWork.Verify(x => x.Get<Security>(), Times.Never);
        provider.Verify(x => x.GetMarketDataAsync(
            It.IsAny<AShareReference>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static StockPriceObservationAppService CreateService(
        IUow unitOfWork,
        IStockDataProvider provider)
        => new(
            unitOfWork,
            provider,
            new SyncStockPriceRequestValidator());

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
        Mock<IRepository<PriceObservation>> observationRepository)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<PriceObservation>())
            .Returns(observationRepository.Object);
        unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }
}
