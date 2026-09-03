using System.Globalization;
using System.Text.Json;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Diagnostics;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Domain.Codes;
using DividendHarvest.Domain.Securities;
using DividendHarvest.Infrastructure.Contracts;
using DividendHarvest.Infrastructure.Exceptions;
using Microsoft.Extensions.Options;

namespace DividendHarvest.Infrastructure.FtShare;

public sealed class FtShareStockDataProvider(
    IFtShareMcpToolInvoker toolInvoker,
    IOptions<FtShareOptions> options,
    IDiagnosticContext diagnosticContext) : IStockDataProvider
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
            reference,
            "profile",
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
            reference,
            "market",
            "FTShare MCP 行情请求超时。",
            "FTShare MCP 行情数据暂时不可用。");
        return ParseMarketData(reference, payload);
    }

    public async Task<IReadOnlyList<StockDividendData>?> GetDividendEventsAsync(
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
            currentOptions.StockDividendEventsToolName,
            arguments,
            cancellationToken,
            reference,
            "dividend",
            "FTShare MCP 股息请求超时。",
            "FTShare MCP 股息数据暂时不可用。");
        return ParseDividendData(reference, payload);
    }

    public async Task<IReadOnlyList<StockFinancialData>?> GetFinancialSnapshotsAsync(
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
            currentOptions.StockFinancialSnapshotsToolName,
            arguments,
            cancellationToken,
            reference,
            "financial",
            "FTShare MCP 财务请求超时。",
            "FTShare MCP 财务数据暂时不可用。");
        return ParseFinancialData(reference, payload);
    }

    private static void ValidateOptions(FtShareOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.StockProfileToolName)
            || string.IsNullOrWhiteSpace(options.StockMarketDataToolName)
            || string.IsNullOrWhiteSpace(options.StockDividendEventsToolName)
            || string.IsNullOrWhiteSpace(options.StockFinancialSnapshotsToolName)
            || string.IsNullOrWhiteSpace(options.SecurityCodeArgumentName)
            || string.IsNullOrWhiteSpace(options.ExchangeCodeArgumentName))
        {
            throw new FtShareProviderException(
                new InvalidOperationException("FTShare MCP 股票资料工具配置不完整。"));
        }
    }

    private async Task<JsonElement?> InvokeToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken,
        AShareReference reference,
        string dataKind,
        string timeoutMessage,
        string unavailableMessage)
    {
        using var diagnosticScope = diagnosticContext.BeginScope(new DiagnosticScope(
            "ftshare_mcp",
            SecurityCode: reference.SecurityCode,
            ExchangeCode: reference.ExchangeCode,
            DataKind: dataKind));

        try
        {
            return await toolInvoker.InvokeAsync(
                toolName,
                arguments,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FtShareProviderException(new TimeoutException(timeoutMessage));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new FtShareProviderException(exception);
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
        var sectorCode = ReadString(
            profile.Value,
            "sector_code",
            "industry_code",
            "sector",
            "industry")?.Trim();

        if (securityName is null || marketCode is null || currencyCode is null)
        {
            return null;
        }

        return new StockData(
            reference.SecurityCode,
            reference.ExchangeCode,
            securityName,
            marketCode,
            currencyCode,
            sectorCode);
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
            "observed_at");
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

        if (sourceRecordId is null || dataSource is null)
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
            dataQualityCode ?? DataQualityCodes.Missing);
    }

    private static IReadOnlyList<StockDividendData>? ParseDividendData(
        AShareReference reference,
        JsonElement? payload)
    {
        if (payload is not { } value)
        {
            return null;
        }

        var items = SelectDividendItems(value);
        if (items is null)
        {
            return null;
        }

        var dividends = new List<StockDividendData>(items.Count);
        foreach (var item in items)
        {
            var dividend = ParseDividendItem(reference, item);
            if (dividend is null)
            {
                return null;
            }

            dividends.Add(dividend);
        }

        return dividends;
    }

    private static IReadOnlyList<StockFinancialData>? ParseFinancialData(
        AShareReference reference,
        JsonElement? payload)
    {
        if (payload is not { } value)
        {
            return null;
        }

        var items = SelectFinancialItems(value);
        if (items is null)
        {
            return null;
        }

        var snapshots = new List<StockFinancialData>(items.Count);
        foreach (var item in items)
        {
            var snapshot = ParseFinancialItem(reference, item);
            if (snapshot is null)
            {
                return null;
            }

            snapshots.Add(snapshot);
        }

        return snapshots;
    }

    private static StockFinancialData? ParseFinancialItem(
        AShareReference reference,
        JsonElement item)
    {
        var returnedCode = ReadString(
            item,
            "security_code",
            "stock_code",
            "code",
            "symbol");
        if (returnedCode is not null
            && NormalizeSecurityCode(returnedCode) != reference.SecurityCode)
        {
            return null;
        }

        var returnedExchange = ReadString(item, "exchange_code", "exchange");
        if (returnedExchange is not null
            && NormalizeExchangeCode(returnedExchange) != reference.ExchangeCode)
        {
            return null;
        }

        var dataAsOfDate = ReadDateOnly(
            item,
            "data_as_of_date",
            "financial_date",
            "period_end_date",
            "date");
        var capturedAt = ReadDateTimeOffset(item, "captured_at", "captured_time")
            ?? DateTimeOffset.UtcNow;
        var publishedAt = ReadDateTimeOffset(item, "published_at", "published_time");
        var dataSource = ReadString(item, "data_source", "source");
        var sourceRecordId = ReadString(
            item,
            "source_record_id",
            "record_id",
            "id");
        var dataQualityCode = ReadString(item, "data_quality_code", "quality_code")
            ?? DataQualityCodes.Missing;
        if (dataAsOfDate is null || dataSource is null || sourceRecordId is null)
        {
            return null;
        }

        return new StockFinancialData(
            reference.SecurityCode,
            reference.ExchangeCode,
            dataAsOfDate.Value,
            capturedAt,
            publishedAt,
            ReadDecimal(item, "earnings_per_share", "eps"),
            ReadDecimal(item, "dividend_payout_ratio", "payout_ratio"),
            ReadDecimal(
                item,
                "three_year_average_dividend_payout_ratio",
                "average_dividend_payout_ratio"),
            ReadDecimal(item, "price_to_book_ratio", "pb"),
            ReadDecimal(item, "return_on_equity", "roe"),
            dataSource,
            sourceRecordId,
            dataQualityCode);
    }

    private static IReadOnlyList<JsonElement>? SelectFinancialItems(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            try
            {
                using var document = JsonDocument.Parse(value.GetString() ?? string.Empty);
                return SelectFinancialItems(document.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().Select(item => item.Clone()).ToArray();
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (HasAnyProperty(
                value,
                "data_as_of_date",
                "financial_date",
                "period_end_date",
                "earnings_per_share",
                "dividend_payout_ratio"))
        {
            return [value.Clone()];
        }

        foreach (var envelopeName in new[] { "data", "result", "financials", "snapshots", "items" })
        {
            if (TryGetProperty(value, envelopeName, out var nested))
            {
                var items = SelectFinancialItems(nested);
                if (items is not null)
                {
                    return items;
                }
            }
        }

        return null;
    }

    private static StockDividendData? ParseDividendItem(
        AShareReference reference,
        JsonElement item)
    {
        var returnedCode = ReadString(
            item,
            "security_code",
            "stock_code",
            "code",
            "symbol");
        if (returnedCode is not null
            && NormalizeSecurityCode(returnedCode) != reference.SecurityCode)
        {
            return null;
        }

        var returnedExchange = ReadString(item, "exchange_code", "exchange");
        if (returnedExchange is not null
            && NormalizeExchangeCode(returnedExchange) != reference.ExchangeCode)
        {
            return null;
        }

        var dividendPerShare = ReadDecimal(
            item,
            "dividend_per_share",
            "cash_dividend_per_share",
            "amount_per_share",
            "dividend_amount");
        var dividendTypeCode = NormalizeDividendType(
            ReadString(item, "dividend_type_code", "dividend_type", "type"),
            ReadBool(item, "is_special_dividend"));
        var dividendStatusCode = NormalizeDividendStatus(
            ReadString(item, "dividend_status_code", "dividend_status", "status"));
        var announcementDate = ReadDateOnly(
            item,
            "announcement_date",
            "announced_date");
        var exDividendDate = ReadDateOnly(
            item,
            "ex_dividend_date",
            "ex_date");
        var paymentDate = ReadDateOnly(
            item,
            "payment_date",
            "paid_date");
        var isSpecialDividend = ReadBool(item, "is_special_dividend")
            ?? string.Equals(dividendTypeCode, "special_cash", StringComparison.Ordinal);
        var publishedAt = ReadDateTimeOffset(item, "published_at", "published_time");
        var capturedAt = ReadDateTimeOffset(item, "captured_at", "captured_time")
            ?? DateTimeOffset.UtcNow;
        var dataSource = ReadString(item, "data_source", "source");
        var sourceRecordId = ReadString(
            item,
            "source_record_id",
            "record_id",
            "id");
        var dataQualityCode = ReadString(item, "data_quality_code", "quality_code")
            ?? DataQualityCodes.Missing;

        if (dividendPerShare is null
            || dividendTypeCode is null
            || dividendStatusCode is null
            || dataSource is null
            || sourceRecordId is null)
        {
            return null;
        }

        return new StockDividendData(
            reference.SecurityCode,
            reference.ExchangeCode,
            dividendPerShare.Value,
            dividendTypeCode,
            dividendStatusCode,
            announcementDate,
            exDividendDate,
            paymentDate,
            isSpecialDividend,
            publishedAt,
            capturedAt,
            dataSource,
            sourceRecordId,
            dataQualityCode);
    }

    private static IReadOnlyList<JsonElement>? SelectDividendItems(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            try
            {
                using var document = JsonDocument.Parse(value.GetString() ?? string.Empty);
                return SelectDividendItems(document.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().Select(item => item.Clone()).ToArray();
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (HasAnyProperty(
                value,
                "dividend_per_share",
                "cash_dividend_per_share",
                "amount_per_share",
                "dividend_amount"))
        {
            return [value.Clone()];
        }

        foreach (var envelopeName in new[] { "data", "result", "dividends", "events", "items" })
        {
            if (TryGetProperty(value, envelopeName, out var nested))
            {
                var items = SelectDividendItems(nested);
                if (items is not null)
                {
                    return items;
                }
            }
        }

        return null;
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

    private static bool? ReadBool(JsonElement value, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(value, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return property.GetBoolean();
            }

            if (property.ValueKind == JsonValueKind.String
                && bool.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? NormalizeDividendType(string? value, bool? isSpecialDividend)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is "special_cash" or "special" or "special cash")
        {
            return "special_cash";
        }

        if (normalized is "regular_cash" or "regular" or "cash" or "regular cash")
        {
            return "regular_cash";
        }

        return isSpecialDividend switch
        {
            true => "special_cash",
            false => "regular_cash",
            _ => null
        };
    }

    private static string? NormalizeDividendStatus(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "implemented" or "paid" or "completed" or "actual" => "implemented",
            "proposed" or "announced" or "pending" => "proposed",
            "cancelled" or "canceled" => "cancelled",
            _ => null
        };
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
