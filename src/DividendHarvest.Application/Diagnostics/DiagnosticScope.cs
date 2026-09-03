namespace DividendHarvest.Application.Diagnostics;

public sealed record DiagnosticScope(
    string Operation,
    string? CorrelationId = null,
    string? RunId = null,
    string? SecurityCode = null,
    string? ExchangeCode = null,
    string? DataKind = null,
    string? ErrorCode = null,
    string? Severity = null);
