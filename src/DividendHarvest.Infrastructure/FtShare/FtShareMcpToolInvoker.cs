using System.Text.Json;
using System.Runtime.ExceptionServices;
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

        Exception? lastTransientException = null;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await InvokeOnceAsync(
                    endpoint,
                    currentOptions,
                    toolName,
                    arguments,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastTransientException = new TimeoutException(
                    "FTShare MCP 工具调用超时。");
            }
            catch (Exception exception) when (IsTransient(exception))
            {
                lastTransientException = exception;
            }

            if (attempt >= Math.Max(currentOptions.MaxRetryCount, 0))
            {
                ExceptionDispatchInfo.Capture(lastTransientException!).Throw();
            }

            await Task.Delay(
                CalculateRetryDelay(currentOptions.RetryDelay, attempt),
                cancellationToken);
        }
    }

    private static async Task<JsonElement?> InvokeOnceAsync(
        Uri endpoint,
        FtShareOptions options,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = endpoint,
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = options.RequestTimeout
            });

        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(options.RequestTimeout);

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

    private static bool IsTransient(Exception exception)
        => exception is HttpRequestException or IOException or TimeoutException;

    private static TimeSpan CalculateRetryDelay(TimeSpan baseDelay, int attempt)
    {
        var multiplier = Math.Min(attempt + 1, 4);
        return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * multiplier);
    }
}
