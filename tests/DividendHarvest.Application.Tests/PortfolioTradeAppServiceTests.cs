using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Portfolio;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using PortfolioEntity = DividendHarvest.Domain.Models.Portfolio;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class PortfolioTradeAppServiceTests
{
    [Fact]
    public async Task RecordAsync_buy_creates_position_updates_cost_and_records_cash()
    {
        var portfolio = CreatePortfolio();
        var security = CreateSecurity();
        var portfolioRepository = CreateRepository([portfolio]);
        var securityRepository = CreateRepository([security]);
        var positionRepository = CreateRepository<PortfolioPosition>([]);
        var tradeRepository = CreateRepository<PortfolioTrade>([]);
        var cashRepository = CreateRepository<CashLedgerEntry>([]);
        SetupAdd(positionRepository);
        SetupAdd(tradeRepository);
        SetupAdd(cashRepository);
        var unitOfWork = CreateUnitOfWork(
            portfolioRepository,
            securityRepository,
            positionRepository,
            tradeRepository,
            cashRepository);
        var service = CreateService(unitOfWork.Object);

        var result = await service.RecordAsync(
            new RecordPortfolioTradeRequest(
                security.SecurityCode,
                security.ExchangeCode,
                new DateOnly(2026, 9, 1),
                "buy",
                100,
                4m,
                5m,
                "trade-1"),
            CancellationToken.None);

        Assert.Equal(100, result.HeldShares);
        Assert.Equal(0, result.CoreShares);
        Assert.Equal(4.05m, result.AverageCostPerShare);
        Assert.Equal(400m, result.TradePrincipalAmount);
        positionRepository.Verify(x => x.AddAsync(
            It.Is<PortfolioPosition>(position =>
                position.HeldShares == 100
                && position.AverageCostPerShare == 4.05m),
            It.IsAny<CancellationToken>()),
            Times.Once);
        cashRepository.Verify(x => x.AddAsync(
            It.Is<CashLedgerEntry>(entry =>
                entry.SourceRecordId!.StartsWith("portfolio_trade:", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_sell_reduces_shares_but_preserves_average_cost()
    {
        var portfolio = CreatePortfolio();
        var security = CreateSecurity();
        var position = new PortfolioPosition
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            HeldShares = 200,
            CoreShares = 100,
            TargetShares = 300,
            AverageCostPerShare = 4m
        };
        var positionRepository = CreateRepository([position]);
        var tradeRepository = CreateRepository<PortfolioTrade>([]);
        var cashRepository = CreateRepository<CashLedgerEntry>([]);
        SetupAdd(tradeRepository);
        SetupAdd(cashRepository);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([portfolio]),
            CreateRepository([security]),
            positionRepository,
            tradeRepository,
            cashRepository);
        var service = CreateService(unitOfWork.Object);

        var result = await service.RecordAsync(
            new RecordPortfolioTradeRequest(
                security.SecurityCode,
                security.ExchangeCode,
                new DateOnly(2026, 9, 1),
                "sell",
                100,
                5m,
                1m,
                "trade-2"),
            CancellationToken.None);

        Assert.Equal(100, result.HeldShares);
        Assert.Equal(4m, result.AverageCostPerShare);
        Assert.Equal(500m, result.TradePrincipalAmount);
        positionRepository.Verify(x => x.AddAsync(
            It.IsAny<PortfolioPosition>(),
            It.IsAny<CancellationToken>()), Times.Never);
        tradeRepository.Verify(x => x.AddAsync(
            It.IsAny<PortfolioTrade>(),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_rejects_sell_that_breaks_core_position_without_commit()
    {
        var portfolio = CreatePortfolio();
        var security = CreateSecurity();
        var positionRepository = CreateRepository([
            new PortfolioPosition
            {
                PortfolioId = portfolio.Id,
                SecurityId = security.Id,
                HeldShares = 200,
                CoreShares = 100,
                AverageCostPerShare = 4m
            }
        ]);
        var tradeRepository = CreateRepository<PortfolioTrade>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([portfolio]),
            CreateRepository([security]),
            positionRepository,
            tradeRepository,
            CreateRepository<CashLedgerEntry>([]));
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<PortfolioTradeValidationException>(() => service.RecordAsync(
            new RecordPortfolioTradeRequest(
                security.SecurityCode,
                security.ExchangeCode,
                new DateOnly(2026, 9, 1),
                "sell",
                201,
                5m,
                0m,
                null),
            CancellationToken.None));

        tradeRepository.Verify(x => x.AddAsync(
            It.IsAny<PortfolioTrade>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_returns_existing_trade_for_a_repeated_source_record()
    {
        var portfolio = CreatePortfolio();
        var security = CreateSecurity();
        var existingTrade = PortfolioTrade.Create(
            portfolio.Id,
            security.Id,
            new DateOnly(2026, 9, 1),
            "buy",
            100,
            4m,
            5m,
            "trade-duplicate");
        var position = new PortfolioPosition
        {
            PortfolioId = portfolio.Id,
            SecurityId = security.Id,
            HeldShares = 100,
            CoreShares = 0,
            TargetShares = 0,
            AverageCostPerShare = 4.05m
        };
        var tradeRepository = CreateRepository([existingTrade]);
        var positionRepository = CreateRepository([position]);
        var cashRepository = CreateRepository<CashLedgerEntry>([]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([portfolio]),
            CreateRepository([security]),
            positionRepository,
            tradeRepository,
            cashRepository);
        var service = CreateService(unitOfWork.Object);

        var result = await service.RecordAsync(
            new RecordPortfolioTradeRequest(
                security.SecurityCode,
                security.ExchangeCode,
                new DateOnly(2026, 9, 1),
                "buy",
                100,
                4m,
                5m,
                " trade-duplicate "),
            CancellationToken.None);

        Assert.Equal(existingTrade.Id, result.PortfolioTradeId);
        Assert.Equal(100, result.HeldShares);
        tradeRepository.Verify(x => x.AddAsync(
            It.IsAny<PortfolioTrade>(),
            It.IsAny<CancellationToken>()), Times.Never);
        cashRepository.Verify(x => x.AddAsync(
            It.IsAny<CashLedgerEntry>(),
            It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_rejects_a_source_record_reused_for_different_trade_data()
    {
        var portfolio = CreatePortfolio();
        var security = CreateSecurity();
        var existingTrade = PortfolioTrade.Create(
            portfolio.Id,
            security.Id,
            new DateOnly(2026, 9, 1),
            "buy",
            100,
            4m,
            5m,
            "trade-conflict");
        var positionRepository = CreateRepository([
            new PortfolioPosition
            {
                PortfolioId = portfolio.Id,
                SecurityId = security.Id,
                HeldShares = 100,
                CoreShares = 0,
                TargetShares = 0,
                AverageCostPerShare = 4.05m
            }
        ]);
        var tradeRepository = CreateRepository([existingTrade]);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([portfolio]),
            CreateRepository([security]),
            positionRepository,
            tradeRepository,
            CreateRepository<CashLedgerEntry>([]));
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<PortfolioTradeConflictException>(() => service.RecordAsync(
            new RecordPortfolioTradeRequest(
                security.SecurityCode,
                security.ExchangeCode,
                new DateOnly(2026, 9, 1),
                "buy",
                200,
                4m,
                5m,
                "trade-conflict"),
            CancellationToken.None));

        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_maps_a_concurrent_source_record_insert_to_a_conflict()
    {
        var portfolio = CreatePortfolio();
        var security = CreateSecurity();
        var positionRepository = CreateRepository<PortfolioPosition>([]);
        var tradeRepository = CreateRepository<PortfolioTrade>([]);
        var cashRepository = CreateRepository<CashLedgerEntry>([]);
        SetupAdd(positionRepository);
        SetupAdd(tradeRepository);
        SetupAdd(cashRepository);
        var unitOfWork = CreateUnitOfWork(
            CreateRepository([portfolio]),
            CreateRepository([security]),
            positionRepository,
            tradeRepository,
            cashRepository);
        unitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate source record"));
        var service = CreateService(unitOfWork.Object);

        await Assert.ThrowsAsync<PortfolioTradeConflictException>(() => service.RecordAsync(
            new RecordPortfolioTradeRequest(
                security.SecurityCode,
                security.ExchangeCode,
                new DateOnly(2026, 9, 1),
                "buy",
                100,
                4m,
                5m,
                "trade-concurrent"),
            CancellationToken.None));
    }

    private static PortfolioTradeAppService CreateService(IUow unitOfWork)
        => new(unitOfWork, new RecordPortfolioTradeRequestValidator());

    private static PortfolioEntity CreatePortfolio()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "长期股息组合"
        };

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

    private static void SetupAdd<TEntity>(Mock<IRepository<TEntity>> repository)
        where TEntity : class
        => repository
            .Setup(x => x.AddAsync(
                It.IsAny<TEntity>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
        Mock<IRepository<PortfolioEntity>> portfolioRepository,
        Mock<IRepository<Security>> securityRepository,
        Mock<IRepository<PortfolioPosition>> positionRepository,
        Mock<IRepository<PortfolioTrade>> tradeRepository,
        Mock<IRepository<CashLedgerEntry>> cashRepository)
    {
        var unitOfWork = new Mock<IUow>();
        unitOfWork.Setup(x => x.Get<PortfolioEntity>()).Returns(portfolioRepository.Object);
        unitOfWork.Setup(x => x.Get<Security>()).Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<PortfolioPosition>())
            .Returns(positionRepository.Object);
        unitOfWork.Setup(x => x.Get<PortfolioTrade>()).Returns(tradeRepository.Object);
        unitOfWork.Setup(x => x.Get<CashLedgerEntry>()).Returns(cashRepository.Object);
        return unitOfWork;
    }
}
