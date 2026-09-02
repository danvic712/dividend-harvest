using System.Linq.Expressions;
using DividendHarvest.Application.Budget;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class BudgetAppServiceTests
{
    [Fact]
    public async Task RecordAsync_saves_a_budget_deposit_without_a_security()
    {
        var portfolio = CreatePortfolio();
        var portfolioRepository = CreateRepository([portfolio]);
        var ledgerRepository = CreateRepository<CashLedgerEntry>([]);
        ledgerRepository
            .Setup(x => x.AddAsync(
                It.IsAny<CashLedgerEntry>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var unitOfWork = CreateUnitOfWork(portfolioRepository, ledgerRepository);
        var service = CreateService(unitOfWork.Object);

        var result = await service.RecordAsync(
            new RecordCashLedgerEntryRequest(
                new DateOnly(2026, 9, 1),
                "budget_deposit",
                "inflow",
                5000m,
                null,
                null,
                "deposit-1"),
            CancellationToken.None);

        Assert.Equal(portfolio.Id, result.PortfolioId);
        Assert.Equal("budget_deposit", result.EntryTypeCode);
        Assert.Equal(5000m, result.CashAmount);
        Assert.Null(result.SecurityCode);
        ledgerRepository.Verify(x => x.AddAsync(
            It.Is<CashLedgerEntry>(entry =>
                entry.PortfolioId == portfolio.Id
                && entry.CashAmount == 5000m
                && entry.SourceRecordId == "deposit-1"),
            It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_requires_a_configured_security_for_stock_related_entries()
    {
        var portfolio = CreatePortfolio();
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([portfolio]),
            CreateRepository<CashLedgerEntry>([]),
            CreateRepository<Security>([]));
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<StockNotConfiguredException>(() => service.RecordAsync(
            new RecordCashLedgerEntryRequest(
                new DateOnly(2026, 9, 1),
                "buy",
                "outflow",
                1000m,
                "000001",
                "SZSE",
                null),
            CancellationToken.None));

        unitOfWork.Verify(x => x.Get<CashLedgerEntry>(), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSummaryAsync_calculates_available_budget_from_cash_direction()
    {
        var portfolio = CreatePortfolio();
        var entries = new[]
        {
            CashLedgerEntry.Create(
                portfolio.Id,
                null,
                new DateOnly(2026, 8, 1),
                "budget_deposit",
                "inflow",
                5000m,
                "deposit-1"),
            CashLedgerEntry.Create(
                portfolio.Id,
                null,
                new DateOnly(2026, 8, 2),
                "fee",
                "outflow",
                10m,
                "fee-1"),
            CashLedgerEntry.Create(
                portfolio.Id,
                null,
                new DateOnly(2026, 8, 3),
                "buy",
                "outflow",
                1000m,
                "buy-1")
        };
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([portfolio]),
            CreateRepository(entries));
        var service = CreateService(unitOfWork.Object);

        var result = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(5000m, result.TotalInflowAmount);
        Assert.Equal(1010m, result.TotalOutflowAmount);
        Assert.Equal(3990m, result.AvailableBudgetAmount);
        Assert.Equal(3, result.EntryCount);
    }

    [Fact]
    public async Task RecordAsync_validates_request_before_accessing_the_database()
    {
        var unitOfWork = new Mock<IUow>();
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<BudgetValidationException>(() => service.RecordAsync(
            new RecordCashLedgerEntryRequest(
                new DateOnly(2026, 9, 1),
                "buy",
                "inflow",
                1000m,
                "000001",
                "SZSE",
                null),
            CancellationToken.None));

        unitOfWork.Verify(x => x.Get<Portfolio>(), Times.Never);
    }

    private static BudgetAppService CreateService(IUow unitOfWork)
        => new(
            unitOfWork,
            new RecordCashLedgerEntryRequestValidator(),
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

    private static Portfolio CreatePortfolio()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "长期股息组合"
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
        Mock<IRepository<Portfolio>> portfolioRepository,
        Mock<IRepository<CashLedgerEntry>> ledgerRepository,
        Mock<IRepository<Security>>? securityRepository = null)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<Portfolio>()).Returns(portfolioRepository.Object);
        unitOfWork.Setup(x => x.Get<CashLedgerEntry>()).Returns(ledgerRepository.Object);
        if (securityRepository is not null)
        {
            unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        }

        return unitOfWork;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
