namespace DividendHarvest.Application.Dtos;

public sealed record SetupResult(
    Guid PortfolioId,
    string PortfolioName,
    IReadOnlyList<SetupStockResult> Stocks);
