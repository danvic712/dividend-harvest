using System.Linq.Expressions;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Models;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockWatchlistAppServiceTests
{
    [Fact]
    public async Task GetAsync_returns_sorted_stocks_with_matching_optional_holdings()
    {
        var firstSecurityId = Guid.NewGuid();
        var secondSecurityId = Guid.NewGuid();
        var securityRepository = CreateRepository<Security>([
            new Security
            {
                Id = firstSecurityId,
                SecurityCode = "600036",
                ExchangeCode = "SSE",
                SecurityName = "招商银行",
                MarketCode = "A-share",
                CurrencyCode = "CNY"
            },
            new Security
            {
                Id = secondSecurityId,
                SecurityCode = "000001",
                ExchangeCode = "SZSE",
                SecurityName = "平安银行",
                MarketCode = "A-share",
                CurrencyCode = "CNY"
            }
        ]);
        var positionRepository = CreateRepository<PortfolioPosition>([
            new PortfolioPosition
            {
                PortfolioId = Guid.NewGuid(),
                SecurityId = firstSecurityId,
                HeldShares = 100,
                CoreShares = 60,
                TargetShares = 200,
                AverageCostPerShare = 10.25m
            }
        ]);
        var unitOfWork = new Mock<IUow>();
        unitOfWork
            .Setup(x => x.Get<Security>())
            .Returns(securityRepository.Object);
        unitOfWork
            .Setup(x => x.Get<PortfolioPosition>())
            .Returns(positionRepository.Object);
        IStockWatchlistAppService service = new StockWatchlistAppService(unitOfWork.Object);

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("000001", result[0].SecurityCode);
        Assert.Equal("平安银行", result[0].SecurityName);
        Assert.Null(result[0].Holding);
        Assert.Equal("600036", result[1].SecurityCode);
        var holding = result[1].Holding;
        Assert.NotNull(holding);
        Assert.Equal(100, holding!.HeldShares);
        Assert.Equal(60, holding!.CoreShares);
        Assert.Equal(10.25m, holding!.AverageCostPerShare);
    }

    private static Mock<IRepository<TEntity>> CreateRepository<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        var repository = new Mock<IRepository<TEntity>>();
        repository
            .Setup(x => x.GetQueryable(
                true,
                It.IsAny<Expression<Func<TEntity, object>>[]>()))
            .Returns(entities.AsAsyncQueryable());
        return repository;
    }
}
