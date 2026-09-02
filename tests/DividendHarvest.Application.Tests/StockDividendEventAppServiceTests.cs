using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Dividends;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockDividendEventAppServiceTests
{
    [Fact]
    public async Task SyncAsync_saves_multiple_dividend_events_for_a_configured_stock()
    {
        var security = CreateSecurity();
        var eventRepository = CreateRepository<DividendEvent>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            eventRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetDividendEventsAsync(
                It.Is<AShareReference>(reference =>
                    reference.SecurityCode == "000001"
                    && reference.ExchangeCode == "SZSE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateDividendData("dividend-1", new DateOnly(2026, 7, 2)),
                CreateDividendData("dividend-2", new DateOnly(2025, 7, 3))
            ]);
        var service = CreateService(unitOfWork.Object, provider.Object);

        var result = await service.SyncAsync(
            new SyncStockDividendsRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("dividend-1", result[0].SourceRecordId);
        eventRepository.Verify(x => x.AddAsync(
            It.Is<DividendEvent>(dividendEvent =>
                dividendEvent.SecurityId == security.Id
                && dividendEvent.DividendPerShare == 0.31m),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_does_not_write_events_that_already_exist()
    {
        var security = CreateSecurity();
        var existing = CreateDividendEvent(security.Id, "dividend-1");
        var eventRepository = CreateRepository([existing]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            eventRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetDividendEventsAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateDividendData("dividend-1", new DateOnly(2026, 7, 2))]);
        var service = CreateService(unitOfWork.Object, provider.Object);

        var result = await service.SyncAsync(
            new SyncStockDividendsRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(existing.Id, result[0].DividendEventId);
        eventRepository.Verify(x => x.AddAsync(
            It.IsAny<DividendEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_rejects_unavailable_dividend_data_without_writing()
    {
        var security = CreateSecurity();
        var eventRepository = CreateRepository<DividendEvent>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            eventRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetDividendEventsAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockDividendData>?)null);
        var service = CreateService(unitOfWork.Object, provider.Object);

        await Assert.ThrowsAsync<StockDividendDataUnavailableException>(() =>
            service.SyncAsync(
                new SyncStockDividendsRequest("000001", "SZSE"),
                CancellationToken.None));

        eventRepository.Verify(x => x.AddAsync(
            It.IsAny<DividendEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static StockDividendEventAppService CreateService(
        IUow unitOfWork,
        IStockDataProvider provider)
        => new(
            unitOfWork,
            provider,
            new SyncStockDividendsRequestValidator());

    private static StockDividendData CreateDividendData(
        string sourceRecordId,
        DateOnly exDividendDate)
        => new(
            "000001",
            "SZSE",
            0.31m,
            "regular_cash",
            "implemented",
            new DateOnly(exDividendDate.Year, 5, 1),
            exDividendDate,
            exDividendDate.AddDays(1),
            false,
            new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            "FTShare",
            sourceRecordId,
            "valid");

    private static DividendEvent CreateDividendEvent(Guid securityId, string sourceRecordId)
        => DividendEvent.Create(
            securityId,
            0.31m,
            "regular_cash",
            "implemented",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 7, 2),
            new DateOnly(2026, 7, 3),
            false,
            new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            "FTShare",
            sourceRecordId,
            "valid");

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
        Mock<IRepository<DividendEvent>> eventRepository)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        unitOfWork.Setup(x => x.Get<DividendEvent>()).Returns(eventRepository.Object);
        unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }
}
