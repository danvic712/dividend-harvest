namespace DividendHarvest.Application.Exceptions;

public abstract class ApplicationValidationException(string message)
    : ApplicationExceptionBase(
        new Dictionary<string, object?>
        {
            ["message"] = message
        });
