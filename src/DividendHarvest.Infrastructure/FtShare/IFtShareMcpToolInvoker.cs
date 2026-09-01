using System.Text.Json;

namespace DividendHarvest.Infrastructure.FtShare;

public interface IFtShareMcpToolInvoker
{
    Task<JsonElement?> InvokeAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken);
}
