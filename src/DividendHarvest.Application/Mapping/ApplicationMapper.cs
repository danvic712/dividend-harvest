using DividendHarvest.Application.Dtos;
using DividendHarvest.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace DividendHarvest.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ApplicationMapper
{
    public static partial StockHoldingSnapshot ToStockHoldingSnapshot(
        PortfolioPosition position);

    [MapProperty(nameof(Security.Id), nameof(StockWatchlistItem.SecurityId))]
    public static partial StockWatchlistItem ToStockWatchlistItem(
        Security security,
        StockHoldingSnapshot? holding);

    [MapProperty(nameof(PriceObservation.Id), nameof(StockPriceObservationResult.PriceObservationId))]
    public static partial StockPriceObservationResult ToStockPriceObservationResult(
        PriceObservation observation,
        string securityCode,
        string exchangeCode);

    [MapProperty(nameof(DividendEvent.Id), nameof(StockDividendEventResult.DividendEventId))]
    public static partial StockDividendEventResult ToStockDividendEventResult(
        DividendEvent dividendEvent,
        string securityCode,
        string exchangeCode);

    [MapProperty(nameof(FinancialSnapshot.Id), nameof(StockFinancialSnapshotResult.FinancialSnapshotId))]
    public static partial StockFinancialSnapshotResult ToStockFinancialSnapshotResult(
        FinancialSnapshot snapshot,
        string securityCode,
        string exchangeCode);

    [MapProperty(nameof(ModelParameterSet.Id), nameof(StockModelParameterSet.ModelParameterSetId))]
    public static partial StockModelParameterSet ToStockModelParameterSet(
        ModelParameterSet parameters,
        string securityCode,
        string exchangeCode);

    [MapProperty(nameof(CashLedgerEntry.Id), nameof(CashLedgerEntryResult.CashLedgerEntryId))]
    public static partial CashLedgerEntryResult ToCashLedgerEntryResult(
        CashLedgerEntry entry,
        string? securityCode,
        string? exchangeCode);

    public static partial SetupStockResult ToSetupStockResult(
        StockData stockData,
        string securityCode,
        string exchangeCode);
}
