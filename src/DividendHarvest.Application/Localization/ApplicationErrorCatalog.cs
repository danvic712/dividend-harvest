using System.Reflection;
using System.Text.Json;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Exceptions;

namespace DividendHarvest.Application.Localization;

public sealed class ApplicationErrorCatalog : IApplicationErrorCatalog
{
    private const string ResourceMarker = ".locales.";
    private const string JsonSuffix = ".json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ApplicationErrorDefinition>> definitions;

    public ApplicationErrorCatalog()
    {
        definitions = LoadDefinitions(typeof(ApplicationErrorCatalog).Assembly);
    }

    public string DefaultCultureName => "zh-CN";

    public IReadOnlyCollection<string> SupportedCultureNames
        => definitions.Keys.Order(StringComparer.Ordinal).ToArray();

    public ApplicationErrorDefinition GetDefinition(
        string cultureName,
        string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        if (!definitions.TryGetValue(cultureName, out var cultureDefinitions)
            || !cultureDefinitions.TryGetValue(errorCode, out var definition))
        {
            throw new InvalidOperationException(
                $"Application error code '{errorCode}' is not defined in locale '{cultureName}'.");
        }

        return definition;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, ApplicationErrorDefinition>> LoadDefinitions(
        Assembly assembly)
    {
        var cultures = new Dictionary<string, Dictionary<string, ApplicationErrorDefinition>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.Contains(ResourceMarker, StringComparison.OrdinalIgnoreCase)
                || !resourceName.EndsWith(JsonSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var (cultureName, domainName) = ParseResourceName(resourceName);
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded application locale resource '{resourceName}' could not be opened.");
            using var document = JsonDocument.Parse(stream);
            EnsureNoDuplicateProperties(document.RootElement, resourceName);
            var domainDefinitions = document.RootElement.Deserialize<
                Dictionary<string, ApplicationErrorDefinition>>(JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Embedded application locale resource '{resourceName}' is empty.");

            if (!cultures.TryGetValue(cultureName, out var cultureDefinitions))
            {
                cultureDefinitions = new Dictionary<string, ApplicationErrorDefinition>(StringComparer.Ordinal);
                cultures.Add(cultureName, cultureDefinitions);
            }

            foreach (var (errorCode, definition) in domainDefinitions)
            {
                if (!cultureDefinitions.TryAdd(errorCode, definition))
                {
                    throw new InvalidOperationException(
                        $"Application error code '{errorCode}' is duplicated in locale '{cultureName}' ({domainName}).");
                }

                if (definition.StatusCode is < 400 or > 599
                    || string.IsNullOrWhiteSpace(definition.Title)
                    || string.IsNullOrWhiteSpace(definition.Detail))
                {
                    throw new InvalidOperationException(
                        $"Application error definition '{errorCode}' in locale '{cultureName}' is invalid.");
                }
            }
        }

        if (cultures.Count == 0 || !cultures.ContainsKey("zh-CN"))
        {
            throw new InvalidOperationException("The embedded zh-CN application error catalog is missing.");
        }

        foreach (var (cultureName, cultureDefinitions) in cultures)
        {
            if (!cultureDefinitions.ContainsKey(ApplicationErrorCodes.Unknown))
            {
                throw new InvalidOperationException(
                    $"Locale '{cultureName}' must define '{ApplicationErrorCodes.Unknown}'.");
            }
        }

        var expectedCodes = ApplicationErrorCodes.All
            .Append(ApplicationErrorCodes.Unknown)
            .Order(StringComparer.Ordinal)
            .ToArray();
        foreach (var (cultureName, cultureDefinitions) in cultures)
        {
            var currentCodes = cultureDefinitions.Keys.Order(StringComparer.Ordinal).ToArray();
            if (!expectedCodes.SequenceEqual(currentCodes, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Locale '{cultureName}' does not define the complete application error catalog.");
            }

            foreach (var errorCode in expectedCodes)
            {
                var defaultDefinition = cultures["zh-CN"][errorCode];
                var currentDefinition = cultureDefinitions[errorCode];
                if (currentDefinition.StatusCode != defaultDefinition.StatusCode
                    || !GetPlaceholders(currentDefinition.Detail).SetEquals(
                        GetPlaceholders(defaultDefinition.Detail)))
                {
                    throw new InvalidOperationException(
                        $"Locale '{cultureName}' does not preserve the HTTP status or placeholders for '{errorCode}'.");
                }
            }
        }

        return cultures.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<string, ApplicationErrorDefinition>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static (string CultureName, string DomainName) ParseResourceName(string resourceName)
    {
        var markerIndex = resourceName.IndexOf(ResourceMarker, StringComparison.OrdinalIgnoreCase);
        var relativeName = resourceName[(markerIndex + ResourceMarker.Length)..^JsonSuffix.Length]
            .Replace('/', '.');
        var segments = relativeName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            throw new InvalidOperationException(
                $"Embedded application locale resource '{resourceName}' must use '{ResourceMarker}<culture>.<domain>.json'.");
        }

        return (segments[0].Replace('_', '-'), segments[^1]);
    }

    private static HashSet<string> GetPlaceholders(string template)
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        var searchStart = 0;
        while (searchStart < template.Length)
        {
            var openingBraceIndex = template.IndexOf('{', searchStart);
            if (openingBraceIndex < 0)
            {
                break;
            }

            var closingBraceIndex = template.IndexOf('}', openingBraceIndex + 1);
            if (closingBraceIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Locale template '{template}' contains an unclosed placeholder.");
            }

            var placeholder = template[
                (openingBraceIndex + 1)..closingBraceIndex].Trim();
            if (placeholder.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Locale template '{template}' contains an invalid placeholder.");
            }

            placeholders.Add(placeholder);

            searchStart = closingBraceIndex + 1;
        }

        return placeholders;
    }

    private static void EnsureNoDuplicateProperties(
        JsonElement element,
        string resourceName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!propertyNames.Add(property.Name))
                {
                    throw new InvalidOperationException(
                        $"Embedded application locale resource '{resourceName}' contains duplicate property '{property.Name}'.");
                }

                EnsureNoDuplicateProperties(property.Value, resourceName);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                EnsureNoDuplicateProperties(item, resourceName);
            }
        }
    }
}
