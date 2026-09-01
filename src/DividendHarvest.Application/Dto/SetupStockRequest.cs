namespace DividendHarvest.Application.Dto;

public sealed record SetupStockRequest(
    string SecurityCode,
    string ExchangeCode,
    InitialHoldingInput? InitialHolding);
