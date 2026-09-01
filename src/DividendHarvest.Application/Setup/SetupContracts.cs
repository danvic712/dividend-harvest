using DividendHarvest.Domain.Portfolio;

namespace DividendHarvest.Application.Setup;

public sealed record SetupRequest(
    string PortfolioName,
    IReadOnlyList<SetupStockRequest> Stocks);

public sealed record SetupStockRequest(
    string SecurityCode,
    string ExchangeCode,
    InitialHoldingInput? InitialHolding);

public sealed record InitialHoldingInput(
    int HeldShares,
    int CoreShares,
    int TargetShares,
    decimal AverageCostPerShare);

public sealed record SetupStatus(
    bool IsComplete,
    IReadOnlyList<string> MissingRequirements);

public sealed record SetupResult(
    Guid PortfolioId,
    string PortfolioName,
    IReadOnlyList<SetupStockResult> Stocks);

public sealed record SetupStockResult(
    string SecurityCode,
    string ExchangeCode,
    string SecurityName);
