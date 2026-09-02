namespace DividendHarvest.Application.Dtos;

public sealed record BudgetSummary(
    Guid PortfolioId,
    string PortfolioName,
    decimal TotalInflowAmount,
    decimal TotalOutflowAmount,
    decimal AvailableBudgetAmount,
    int EntryCount,
    DateTimeOffset ComputedAt);
