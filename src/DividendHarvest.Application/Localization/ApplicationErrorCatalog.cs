using System.Globalization;
using System.Reflection;
using System.Text.Json;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Exceptions;

namespace DividendHarvest.Application.Localization;

public sealed class ApplicationErrorCatalog : IApplicationErrorCatalog
{
    private const string ResourceMarker = ".locales.";
    private const string JsonSuffix = ".json";
    private const string UnknownErrorCode = "application_error_unknown";

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

    public LocalizedApplicationError Resolve(
        ApplicationExceptionBase exception,
        string? acceptLanguage = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var cultureName = SelectCulture(acceptLanguage);
        var cultureDefinitions = definitions[cultureName];
        var errorCode = cultureDefinitions.ContainsKey(exception.ErrorCode)
            ? exception.ErrorCode
            : UnknownErrorCode;
        var definition = cultureDefinitions[errorCode];

        return new LocalizedApplicationError(
            errorCode,
            cultureName,
            definition.StatusCode,
            definition.Title,
            Interpolate(definition.Detail, exception.Parameters, cultureName));
    }

    private string SelectCulture(string? acceptLanguage)
    {
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            foreach (var language in acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var languageName = language.Split(';', 2)[0].Trim();
                if (definitions.ContainsKey(languageName))
                {
                    return languageName;
                }

                var neutralLanguage = languageName.Split('-', 2)[0];
                var matchingCulture = definitions.Keys.FirstOrDefault(
                    cultureName => cultureName.StartsWith(
                        neutralLanguage + "-",
                        StringComparison.OrdinalIgnoreCase));
                if (matchingCulture is not null)
                {
                    return matchingCulture;
                }
            }
        }

        return DefaultCultureName;
    }

    private static string Interpolate(
        string template,
        IReadOnlyDictionary<string, object?> parameters,
        string cultureName)
    {
        var result = template;
        var culture = CultureInfo.GetCultureInfo(cultureName);

        foreach (var parameter in parameters)
        {
            var value = parameter.Value switch
            {
                null => string.Empty,
                DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, culture),
                _ => parameter.Value.ToString() ?? string.Empty
            };
            result = result.Replace(
                "{" + parameter.Key + "}",
                value,
                StringComparison.Ordinal);
        }

        return result;
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
            var domainDefinitions = JsonSerializer.Deserialize<Dictionary<string, ApplicationErrorDefinition>>(
                stream,
                JsonOptions)
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
            if (!cultureDefinitions.ContainsKey(UnknownErrorCode))
            {
                throw new InvalidOperationException(
                    $"Locale '{cultureName}' must define '{UnknownErrorCode}'.");
            }
        }

        var defaultCodes = cultures["zh-CN"].Keys.Order(StringComparer.Ordinal).ToArray();
        foreach (var (cultureName, cultureDefinitions) in cultures)
        {
            var currentCodes = cultureDefinitions.Keys.Order(StringComparer.Ordinal).ToArray();
            if (!defaultCodes.SequenceEqual(currentCodes, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Locale '{cultureName}' does not define the same application error codes as zh-CN.");
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
}
