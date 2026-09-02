namespace DividendHarvest.Application.Dtos;

public sealed record RecordCashLedgerEntryRequest(
    DateOnly EntryDate,
    string EntryTypeCode,
    string CashDirectionCode,
    decimal CashAmount,
    string? SecurityCode,
    string? ExchangeCode,
    string? SourceRecordId);
