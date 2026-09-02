namespace DividendHarvest.Application.Dtos;

public sealed record SetupRequest(
    string PortfolioName,
    IReadOnlyList<SetupStockRequest> Stocks);
