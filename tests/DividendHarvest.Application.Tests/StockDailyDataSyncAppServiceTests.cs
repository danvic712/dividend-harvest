using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using Moq;
using Xunit;

namespace DividendHarvest.Application.Tests;

public sealed class StockDailyDataSyncAppServiceTests
{
    [Fact]
    public void Daily_sync_schedule_skips_weekend_and_returns_next_local_run_time()
    {
        var utcNow = new DateTimeOffset(
            2026,
            9,
            4,
            20,
            0,
            0,
            TimeSpan.Zero);

        var nextRun = DailySyncSchedule.GetNextRunUtc(
            utcNow,
            new TimeOnly(18, 0),
            TimeZoneInfo.Utc);

        Assert.Equal(
            new DateTimeOffset(2026, 9, 7, 18, 0, 0, TimeSpan.Zero),
            nextRun);
    }

    [Fact]
    public void Daily_sync_schedule_returns_same_day_when_before_run_time()
    {
        var utcNow = new DateTimeOffset(
            2026,
            9,
            2,
            9,
            0,
            0,
            TimeSpan.Zero);

        var nextRun = DailySyncSchedule.GetNextRunUtc(
            utcNow,
            new TimeOnly(18, 0),
            TimeZoneInfo.Utc);

        Assert.Equal(
            new DateTimeOffset(2026, 9, 2, 18, 0, 0, TimeSpan.Zero),
            nextRun);
    }

    [Fact]
    public async Task SyncAsync_updates_all_data_kinds_for_each_watchlist_stock()
    {
        var stocks = new[]
        {
            CreateStock("000001", "SZSE"),
            CreateStock("600001", "SSE")
        };
        var watchlist = new Mock<IStockWatchlistAppService>();
        watchlist
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);
        var prices = new Mock<IStockPriceObservationAppService>();
        prices
            .Setup(x => x.SyncAsync(
                It.IsAny<SyncStockPriceRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockPriceObservationResult)null!);
        var dividends = new Mock<IStockDividendEventAppService>();
        dividends
            .Setup(x => x.SyncAsync(
                It.IsAny<SyncStockDividendsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockDividendEventResult>());
        var financials = new Mock<IStockFinancialSnapshotAppService>();
        financials
            .Setup(x => x.SyncAsync(
                It.IsAny<SyncStockFinancialsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockFinancialSnapshotResult>());
        var service = CreateService(
            watchlist.Object,
            prices.Object,
            dividends.Object,
            financials.Object);

        var result = await service.SyncAsync(CancellationToken.None);

        Assert.Equal(2, result.AttemptedStockCount);
        Assert.Equal(2, result.FullyCompletedStockCount);
        Assert.Equal(0, result.PartiallyFailedStockCount);
        Assert.Empty(result.Failures);
        prices.Verify(x => x.SyncAsync(
            It.IsAny<SyncStockPriceRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        dividends.Verify(x => x.SyncAsync(
            It.IsAny<SyncStockDividendsRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        financials.Verify(x => x.SyncAsync(
            It.IsAny<SyncStockFinancialsRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncAsync_continues_after_one_data_kind_fails()
    {
        var stock = CreateStock("000001", "SZSE");
        var watchlist = new Mock<IStockWatchlistAppService>();
        watchlist
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([stock]);
        var prices = new Mock<IStockPriceObservationAppService>();
        prices
            .Setup(x => x.SyncAsync(
                It.IsAny<SyncStockPriceRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StockMarketDataUnavailableException("000001"));
        var dividends = new Mock<IStockDividendEventAppService>();
        dividends
            .Setup(x => x.SyncAsync(
                It.IsAny<SyncStockDividendsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockDividendEventResult>());
        var financials = new Mock<IStockFinancialSnapshotAppService>();
        financials
            .Setup(x => x.SyncAsync(
                It.IsAny<SyncStockFinancialsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockFinancialSnapshotResult>());
        var service = CreateService(
            watchlist.Object,
            prices.Object,
            dividends.Object,
            financials.Object);

        var result = await service.SyncAsync(CancellationToken.None);

        Assert.Equal(1, result.AttemptedStockCount);
        Assert.Equal(0, result.FullyCompletedStockCount);
        Assert.Equal(1, result.PartiallyFailedStockCount);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("price", failure.DataKind);
        Assert.Equal("stock_market_data_unavailable", failure.ErrorCode);
        dividends.Verify(x => x.SyncAsync(
            It.IsAny<SyncStockDividendsRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        financials.Verify(x => x.SyncAsync(
            It.IsAny<SyncStockFinancialsRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static StockDailyDataSyncAppService CreateService(
        IStockWatchlistAppService watchlist,
        IStockPriceObservationAppService prices,
        IStockDividendEventAppService dividends,
        IStockFinancialSnapshotAppService financials)
        => new(
            watchlist,
            prices,
            dividends,
            financials,
            new FixedTimeProvider(
                new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero)));

    private static StockWatchlistItem CreateStock(
        string securityCode,
        string exchangeCode)
        => new(
            securityCode,
            exchangeCode,
            "测试股票",
            "A-share",
            "CNY",
            null);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
