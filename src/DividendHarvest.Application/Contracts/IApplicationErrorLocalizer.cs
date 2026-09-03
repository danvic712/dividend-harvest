using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Localization;

namespace DividendHarvest.Application.Contracts;

public interface IApplicationErrorLocalizer
{
    LocalizedApplicationError Localize(
        ApplicationExceptionBase exception,
        string? acceptLanguage = null);
}
