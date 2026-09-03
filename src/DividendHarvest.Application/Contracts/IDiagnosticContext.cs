using DividendHarvest.Application.Diagnostics;

namespace DividendHarvest.Application.Contracts;

public interface IDiagnosticContext
{
    IDisposable BeginScope(DiagnosticScope scope);
}
