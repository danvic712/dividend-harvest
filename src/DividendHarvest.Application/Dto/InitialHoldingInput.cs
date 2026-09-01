namespace DividendHarvest.Application.Dto;

public sealed record InitialHoldingInput(
    int HeldShares,
    int CoreShares,
    int TargetShares,
    decimal AverageCostPerShare);
