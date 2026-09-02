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
        if (!cultureDefinitions.TryGetValue(exception.ErrorCode, out var definition))
        {
            throw new InvalidOperationException(
                $"Application error code '{exception.ErrorCode}' is not defined in locale '{cultureName}'.");
        }

        return new LocalizedApplicationError(
            exception.ErrorCode,
            cultureName,
            definition.StatusCode,
            definition.Title,
            Interpolate(
                definition.Detail,
                GetInterpolationParameters(exception, definition),
                cultureName));
    }

    private string SelectCulture(string? acceptLanguage)
    {
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            var preferences = acceptLanguage
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select((language, index) => ParseLanguagePreference(language, index))
                .Where(preference => preference.Quality > 0)
                .OrderByDescending(preference => preference.Quality)
                .ThenBy(preference => preference.Index);

            foreach (var preference in preferences)
            {
                var languageName = preference.LanguageName;
                if (languageName == "*")
                {
                    continue;
                }

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

    private static (string LanguageName, double Quality, int Index) ParseLanguagePreference(
        string language,
        int index)
    {
        var segments = language.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var languageName = segments[0].Trim();
        var quality = 1d;
        var qualityParameter = segments
            .Skip(1)
            .Select(segment => segment.Trim())
            .FirstOrDefault(segment => segment.StartsWith("q=", StringComparison.OrdinalIgnoreCase));

        if (qualityParameter is not null
            && (!double.TryParse(
                qualityParameter[2..],
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out quality)
                || quality is < 0 or > 1))
        {
            quality = 0;
        }

        return (languageName, quality, index);
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

    private static IReadOnlyDictionary<string, object?> GetInterpolationParameters(
        ApplicationExceptionBase exception,
        ApplicationErrorDefinition definition)
    {
        if (exception is not ApplicationValidationException
            || string.IsNullOrWhiteSpace(definition.ValidationMessage))
        {
            return exception.Parameters;
        }

        var parameters = exception.Parameters.ToDictionary(
            parameter => parameter.Key,
            parameter => parameter.Value,
            StringComparer.Ordinal);
        parameters["message"] = definition.ValidationMessage;
        return parameters;
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

        var exceptionCodes = assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ApplicationExceptionBase).IsAssignableFrom(type))
            .Select(type =>
            {
                var attribute = type.GetCustomAttribute<ApplicationErrorCodeAttribute>()
                    ?? throw new InvalidOperationException(
                        $"Application exception '{type.FullName}' must declare {nameof(ApplicationErrorCodeAttribute)}.");
                return (Type: type, ErrorCode: attribute.ErrorCode);
            })
            .ToArray();
        var duplicateExceptionCodes = exceptionCodes
            .GroupBy(item => item.ErrorCode, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateExceptionCodes is not null)
        {
            throw new InvalidOperationException(
                $"Application error code '{duplicateExceptionCodes.Key}' is used by multiple exception types.");
        }

        var expectedCodes = exceptionCodes
            .Select(item => item.ErrorCode)
            .Append(UnknownErrorCode)
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
}
