namespace DividendHarvest.Application.Dtos;

public sealed record SetupStockRequest(
    string SecurityCode,
    string ExchangeCode,
    InitialHoldingInput? InitialHolding);
