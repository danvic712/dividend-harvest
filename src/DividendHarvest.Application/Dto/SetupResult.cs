namespace DividendHarvest.Application.Dto;

public sealed record SetupResult(
    Guid PortfolioId,
    string PortfolioName,
    IReadOnlyList<SetupStockResult> Stocks);
