namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("model_parameter_validation_failed")]
public sealed class ModelParameterValidationException(string message)
    : ApplicationValidationException(message);
