namespace DividendHarvest.Application.Dtos;

public sealed record InitialHoldingInput(
    int HeldShares,
    int CoreShares,
    int TargetShares,
    decimal AverageCostPerShare);
