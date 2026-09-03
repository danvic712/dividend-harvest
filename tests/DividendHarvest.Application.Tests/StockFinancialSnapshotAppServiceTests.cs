using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Localization;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockFinancialSnapshotAppServiceTests
{
    [Fact]
    public async Task SyncAsync_saves_a_financial_snapshot_for_a_configured_stock()
    {
        var security = CreateSecurity();
        var snapshotRepository = CreateRepository<FinancialSnapshot>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            snapshotRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetFinancialSnapshotsAsync(
                It.Is<AShareReference>(reference =>
                    reference.SecurityCode == "000001"
                    && reference.ExchangeCode == "SZSE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new StockFinancialData(
                    "000001",
                    "SZSE",
                    new DateOnly(2026, 6, 30),
                    new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
                    null,
                    1.20m,
                    0.45m,
                    0.40m,
                    0.90m,
                    0.12m,
                    "FTShare",
                    "financial-1",
                    "valid")
            ]);
        var service = CreateService(unitOfWork.Object, provider.Object);

        var result = await service.SyncAsync(
            new SyncStockFinancialsRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("financial-1", result[0].SourceRecordId);
        Assert.Equal(0.45m, result[0].DividendPayoutRatio);
        snapshotRepository.Verify(x => x.AddAsync(
            It.Is<FinancialSnapshot>(snapshot =>
                snapshot.SecurityId == security.Id
                && snapshot.DataAsOfDate == new DateOnly(2026, 6, 30)
                && snapshot.DividendPayoutRatio == 0.45m),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_does_not_write_an_existing_data_as_of_date_twice()
    {
        var security = CreateSecurity();
        var existing = CreateSnapshot(security.Id);
        var snapshotRepository = CreateRepository([existing]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([security]),
            snapshotRepository);
        var provider = new Mock<IStockDataProvider>();
        provider
            .Setup(x => x.GetFinancialSnapshotsAsync(
                It.IsAny<AShareReference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new StockFinancialData(
                    "000001",
                    "SZSE",
                    new DateOnly(2026, 6, 30),
                    new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
                    null,
                    1.20m,
                    0.45m,
                    0.40m,
                    0.90m,
                    0.12m,
                    "FTShare",
                    "financial-1",
                    "valid")
            ]);
        var service = CreateService(unitOfWork.Object, provider.Object);

        var result = await service.SyncAsync(
            new SyncStockFinancialsRequest("000001", "SZSE"),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(existing.Id, result[0].FinancialSnapshotId);
        snapshotRepository.Verify(x => x.AddAsync(
            It.IsAny<FinancialSnapshot>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static StockFinancialSnapshotAppService CreateService(
        IUow unitOfWork,
        IStockDataProvider provider)
        => new(
            new StockFactSyncAppService(
                unitOfWork,
                provider),
            new SyncStockFinancialsRequestValidator());

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

    private static FinancialSnapshot CreateSnapshot(Guid securityId)
        => FinancialSnapshot.Create(
            securityId,
            new DateOnly(2026, 6, 30),
            new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero),
            null,
            1.20m,
            0.45m,
            0.40m,
            0.90m,
            0.12m,
            "FTShare",
            "financial-1",
            "valid");

    private static Mock<IRepository<TEntity>> CreateRepository<TEntity>(
        IEnumerable<TEntity> entities)
        where TEntity : class
        => RepositoryMock.Create(entities);

    private static Mock<IUow> CreateUnitOfWork(
        Mock<IRepository<Security>> securityRepository,
        Mock<IRepository<FinancialSnapshot>> snapshotRepository)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<FinancialSnapshot>())
            .Returns(snapshotRepository.Object);
        unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }
}
