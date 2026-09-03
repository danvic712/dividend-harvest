using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Diagnostics;
using Serilog.Context;

namespace DividendHarvest.Diagnostics;

public sealed class SerilogDiagnosticContext : IDiagnosticContext
{
    private static readonly HashSet<string> AllowedOperations =
    [
        "http_request",
        "http_error",
        "daily_stock_data_sync",
        "ftshare_mcp"
    ];

    private static readonly HashSet<string> AllowedDataKinds =
    [
        "profile",
        "market",
        "dividend",
        "financial"
    ];

    public IDisposable BeginScope(DiagnosticScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var properties = new List<IDisposable>();
        Push(properties, "diagnostic_operation", scope.Operation, AllowedOperations);
        Push(properties, "correlation_id", scope.CorrelationId);
        Push(properties, "run_id", scope.RunId);
        Push(properties, "security_code", scope.SecurityCode);
        Push(properties, "exchange_code", scope.ExchangeCode);
        Push(properties, "data_kind", scope.DataKind, AllowedDataKinds);
        Push(properties, "error_code", scope.ErrorCode);
        Push(properties, "severity", scope.Severity);

        return new CompositeDisposable(properties);
    }

    private static void Push(
        ICollection<IDisposable> properties,
        string propertyName,
        string? value,
        IReadOnlySet<string>? allowedValues = null)
    {
        var safeValue = Sanitize(value);
        if (safeValue is not null
            && (allowedValues is null || allowedValues.Contains(safeValue)))
        {
            properties.Add(LogContext.PushProperty(propertyName, safeValue));
        }
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 128
            || value.Any(character =>
                !char.IsLetterOrDigit(character)
                && character is not '-'
                and not '_'
                and not '.'
                and not ':'))
        {
            return null;
        }

        return value;
    }

    private sealed class CompositeDisposable(IReadOnlyList<IDisposable> properties) : IDisposable
    {
        public void Dispose()
        {
            for (var index = properties.Count - 1; index >= 0; index--)
            {
                properties[index].Dispose();
            }
        }
    }
}
