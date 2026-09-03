using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Localization;

namespace DividendHarvest.Application.Contracts;

public interface IApplicationErrorCatalog
{
    string DefaultCultureName { get; }

    IReadOnlyCollection<string> SupportedCultureNames { get; }

    ApplicationErrorDefinition GetDefinition(
        string cultureName,
        string errorCode);
}
