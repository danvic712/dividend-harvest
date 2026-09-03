namespace DividendHarvest.Application.Exceptions;

public sealed class ApplicationValidationException(
    string errorCode,
    string message)
    : ApplicationExceptionBase(
        errorCode,
        new Dictionary<string, object?>
        {
            ["message"] = message
        });
