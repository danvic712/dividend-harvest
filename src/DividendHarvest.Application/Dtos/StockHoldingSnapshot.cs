namespace DividendHarvest.Application.Dtos;

public sealed record StockHoldingSnapshot(
    int HeldShares,
    int CoreShares,
    int TargetShares,
    decimal AverageCostPerShare);
