namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("model_parameter_version_already_exists")]
public sealed class ModelParameterVersionAlreadyExistsException(
    string securityCode,
    DateOnly effectiveFromDate)
    : ApplicationExceptionBase(
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode,
            ["effectiveFromDate"] = effectiveFromDate
        });
