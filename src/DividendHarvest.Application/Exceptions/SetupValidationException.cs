namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("setup_validation_failed")]
public sealed class SetupValidationException(string message)
    : ApplicationValidationException(message);
