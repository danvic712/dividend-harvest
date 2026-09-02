namespace DividendHarvest.Application.Dtos;

public sealed record CashLedgerEntryResult(
    Guid CashLedgerEntryId,
    Guid PortfolioId,
    DateOnly EntryDate,
    string EntryTypeCode,
    string CashDirectionCode,
    decimal CashAmount,
    string? SecurityCode,
    string? ExchangeCode,
    string? SourceRecordId);
