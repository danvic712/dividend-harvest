using System.Globalization;
using System.Text.Json;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Domain.Securities;
using DividendHarvest.Infrastructure.Contracts;
using Microsoft.Extensions.Options;

namespace DividendHarvest.Infrastructure.FtShare;

public sealed class FtShareStockDataProvider(
    IFtShareMcpToolInvoker toolInvoker,
    IOptions<FtShareOptions> options) : IStockDataProvider
{
    public async Task<StockData?> GetAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var currentOptions = options.Value;
        ValidateOptions(currentOptions);
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [currentOptions.SecurityCodeArgumentName] = reference.SecurityCode,
            [currentOptions.ExchangeCodeArgumentName] = reference.ExchangeCode
        };

        var payload = await InvokeToolAsync(
            currentOptions.StockProfileToolName,
            arguments,
            cancellationToken,
            "FTShare MCP 请求超时。",
            "FTShare MCP 股票资料暂时不可用。");
        return ParseStockData(reference, payload);
    }

    public async Task<StockMarketData?> GetMarketDataAsync(
        AShareReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var currentOptions = options.Value;
        ValidateOptions(currentOptions);
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [currentOptions.SecurityCodeArgumentName] = reference.SecurityCode,
            [currentOptions.ExchangeCodeArgumentName] = reference.ExchangeCode
        };

        var payload = await InvokeToolAsync(
            currentOptions.StockMarketDataToolName,
            arguments,
            cancellationToken,
            "FTShare MCP 行情请求超时。",
            "FTShare MCP 行情数据暂时不可用。");
        return ParseMarketData(reference, payload);
    }

    private static void ValidateOptions(FtShareOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.StockProfileToolName)
            || string.IsNullOrWhiteSpace(options.StockMarketDataToolName)
            || string.IsNullOrWhiteSpace(options.SecurityCodeArgumentName)
            || string.IsNullOrWhiteSpace(options.ExchangeCodeArgumentName))
        {
            throw new StockDataProviderUnavailableException(
                "FTShare MCP 股票资料工具配置不完整。",
                new InvalidOperationException("FTShare MCP 股票资料工具配置不完整。"));
        }
    }

    private async Task<JsonElement?> InvokeToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken,
        string timeoutMessage,
        string unavailableMessage)
    {
        try
        {
            return await toolInvoker.InvokeAsync(
                toolName,
                arguments,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new StockDataProviderUnavailableException(
                timeoutMessage,
                new TimeoutException(timeoutMessage));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new StockDataProviderUnavailableException(
                unavailableMessage,
                exception);
        }
    }

    private static StockData? ParseStockData(
        AShareReference reference,
        JsonElement? payload)
    {
        if (payload is not { } value)
        {
            return null;
        }

        var profile = SelectPayload(value);
        if (profile is not { ValueKind: JsonValueKind.Object })
        {
            return null;
        }

        var returnedCode = ReadString(profile.Value, "security_code", "stock_code", "code", "symbol");
        if (returnedCode is not null && NormalizeSecurityCode(returnedCode) != reference.SecurityCode)
        {
            return null;
        }

        var returnedExchange = ReadString(profile.Value, "exchange_code", "exchange");
        if (returnedExchange is not null
            && NormalizeExchangeCode(returnedExchange) != reference.ExchangeCode)
        {
            return null;
        }

        var securityName = ReadString(profile.Value, "security_name", "stock_name", "company_name", "name");
        var marketCode = NormalizeMarketCode(ReadString(profile.Value, "market_code", "market", "market_type"));
        var currencyCode = NormalizeCurrencyCode(ReadString(profile.Value, "currency_code", "currency"));

        if (securityName is null || marketCode is null || currencyCode is null)
        {
            return null;
        }

        return new StockData(
            reference.SecurityCode,
            reference.ExchangeCode,
            securityName,
            marketCode,
            currencyCode);
    }

    private static JsonElement? SelectPayload(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            try
            {
                using var document = JsonDocument.Parse(value.GetString() ?? string.Empty);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (HasAnyProperty(
                value,
                "security_name",
                "stock_name",
                "company_name",
                "name",
                "close_price",
                "closing_price",
                "last_price",
                "price",
                "trading_date",
                "data_as_of_date"))
        {
            return value;
        }

        foreach (var envelopeName in new[] { "data", "result", "profile", "security" })
        {
            if (TryGetProperty(value, envelopeName, out var nested))
            {
                var profile = SelectPayload(nested);
                if (profile is not null)
                {
                    return profile;
                }
            }
        }

        return null;
    }

    private static StockMarketData? ParseMarketData(
        AShareReference reference,
        JsonElement? payload)
    {
        if (payload is not { } value)
        {
            return null;
        }

        var marketData = SelectPayload(value);
        if (marketData is not { ValueKind: JsonValueKind.Object })
        {
            return null;
        }

        var returnedCode = ReadString(
            marketData.Value,
            "security_code",
            "stock_code",
            "code",
            "symbol");
        if (returnedCode is not null
            && NormalizeSecurityCode(returnedCode) != reference.SecurityCode)
        {
            return null;
        }

        var returnedExchange = ReadString(
            marketData.Value,
            "exchange_code",
            "exchange");
        if (returnedExchange is not null
            && NormalizeExchangeCode(returnedExchange) != reference.ExchangeCode)
        {
            return null;
        }

        var closePrice = ReadDecimal(
            marketData.Value,
            "close_price",
            "closing_price",
            "last_price",
            "price");
        var tradingDate = ReadDateOnly(
            marketData.Value,
            "trading_date",
            "data_as_of_date",
            "as_of_date",
            "date");
        var priceObservedAt = ReadDateTimeOffset(
            marketData.Value,
            "price_observed_at",
            "observed_at",
            "captured_at");
        if (closePrice is null || tradingDate is null || priceObservedAt is null)
        {
            return null;
        }

        var sourceRecordId = ReadString(
            marketData.Value,
            "source_record_id",
            "record_id",
            "id");
        var dataSource = ReadString(
            marketData.Value,
            "data_source",
            "source");
        var dataQualityCode = ReadString(
            marketData.Value,
            "data_quality_code",
            "quality_code");

        if (sourceRecordId is null || dataSource is null || dataQualityCode is null)
        {
            return null;
        }

        return new StockMarketData(
            reference.SecurityCode,
            reference.ExchangeCode,
            closePrice.Value,
            tradingDate.Value,
            priceObservedAt.Value,
            dataSource,
            sourceRecordId,
            dataQualityCode);
    }

    private static string? ReadString(JsonElement value, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(value, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var text = property.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
            else if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return property.ToString();
            }
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement value, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(value, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number
                && property.TryGetDecimal(out var number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String
                && decimal.TryParse(
                    property.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out number))
            {
                return number;
            }
        }

        return null;
    }

    private static DateOnly? ReadDateOnly(
        JsonElement value,
        params string[] propertyNames)
    {
        var text = ReadString(value, propertyNames);
        return text is not null
            && DateOnly.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date)
            ? date
            : null;
    }

    private static DateTimeOffset? ReadDateTimeOffset(
        JsonElement value,
        params string[] propertyNames)
    {
        var text = ReadString(value, propertyNames);
        return text is not null
            && DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var observedAt)
            ? observedAt
            : null;
    }

    private static string NormalizeSecurityCode(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length < 6 && trimmed.All(character => character is >= '0' and <= '9')
            ? trimmed.PadLeft(6, '0')
            : trimmed;
    }

    private static string NormalizeExchangeCode(string value) => value.Trim().ToUpperInvariant() switch
    {
        "SH" or "SSE" => "SSE",
        "SZ" or "SZSE" => "SZSE",
        "BJ" or "BSE" => "BSE",
        _ => value.Trim().ToUpperInvariant()
    };

    private static string? NormalizeMarketCode(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "A 股" or "A股" or "A-SHARE" or "A_SHARE" or "ASHARE" => "A-share",
        _ => null
    };

    private static string? NormalizeCurrencyCode(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "CNY" or "RMB" or "人民币" => "CNY",
        _ => null
    };

    private static bool HasAnyProperty(JsonElement value, params string[] propertyNames) =>
        propertyNames.Any(propertyName => TryGetProperty(value, propertyName, out _));

    private static bool TryGetProperty(
        JsonElement value,
        string propertyName,
        out JsonElement property)
    {
        foreach (var candidate in value.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }
}
