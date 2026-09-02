using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Localization;

namespace DividendHarvest.Application.Contracts;

public interface IApplicationErrorCatalog
{
    string DefaultCultureName { get; }

    IReadOnlyCollection<string> SupportedCultureNames { get; }

    LocalizedApplicationError Resolve(
        ApplicationExceptionBase exception,
        string? acceptLanguage = null);
}
