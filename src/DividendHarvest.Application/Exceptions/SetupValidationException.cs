namespace DividendHarvest.Application.Exceptions;

public sealed class SetupValidationException(string message)
    : ApplicationValidationException("setup_validation_failed", message);
