namespace DividendHarvest.Application.Dto;

public sealed record SetupRequest(
    string PortfolioName,
    IReadOnlyList<SetupStockRequest> Stocks);
