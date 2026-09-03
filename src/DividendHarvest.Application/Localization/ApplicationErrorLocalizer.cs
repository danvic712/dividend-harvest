using System.Globalization;
using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Exceptions;

namespace DividendHarvest.Application.Localization;

public sealed class ApplicationErrorLocalizer(
    IApplicationErrorCatalog catalog) : IApplicationErrorLocalizer
{
    public LocalizedApplicationError Localize(
        ApplicationExceptionBase exception,
        string? acceptLanguage = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var cultureName = SelectCulture(acceptLanguage);
        var definition = catalog.GetDefinition(cultureName, exception.ErrorCode);
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

                var exactCulture = catalog.SupportedCultureNames.FirstOrDefault(
                    cultureName => string.Equals(
                        cultureName,
                        languageName,
                        StringComparison.OrdinalIgnoreCase));
                if (exactCulture is not null)
                {
                    return exactCulture;
                }

                var neutralLanguage = languageName.Split('-', 2)[0];
                var matchingCulture = catalog.SupportedCultureNames.FirstOrDefault(
                    cultureName => cultureName.StartsWith(
                        neutralLanguage + "-",
                        StringComparison.OrdinalIgnoreCase));
                if (matchingCulture is not null)
                {
                    return matchingCulture;
                }
            }
        }

        return catalog.DefaultCultureName;
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
}
