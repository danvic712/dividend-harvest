using System.Text.Json;
using DividendHarvest.Infrastructure.Contracts;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Options;

namespace DividendHarvest.Infrastructure.FtShare;

public sealed class FtShareMcpToolInvoker(
    IOptions<FtShareOptions> options) : IFtShareMcpToolInvoker
{
    public async Task<JsonElement?> InvokeAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(arguments);

        var currentOptions = options.Value;
        if (!Uri.TryCreate(currentOptions.McpEndpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "FTShare MCP 地址未配置，或不是有效的 HTTP(S) 地址。请设置 FtShare__McpEndpoint。");
        }

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = endpoint,
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = currentOptions.RequestTimeout
            });

        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(currentOptions.RequestTimeout);

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: timeoutCancellation.Token);

        var result = await client.CallToolAsync(
            toolName,
            arguments,
            progress: null,
            options: null,
            timeoutCancellation.Token);

        if (result.IsError == true)
        {
            var message = result.Content
                .OfType<TextContentBlock>()
                .Select(content => content.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text))
                ?? "FTShare MCP 工具调用失败。";

            throw new McpException(message);
        }

        if (result.StructuredContent is { } structuredContent)
        {
            return structuredContent.Clone();
        }

        foreach (var textContent in result.Content.OfType<TextContentBlock>())
        {
            try
            {
                using var document = JsonDocument.Parse(textContent.Text);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                // A text-only response is not a stock profile payload.
            }
        }

        return null;
    }
}
