namespace DividendHarvest.Application.Exceptions;

public sealed class ModelParameterValidationException(string message)
    : ApplicationValidationException("model_parameter_validation_failed", message);
