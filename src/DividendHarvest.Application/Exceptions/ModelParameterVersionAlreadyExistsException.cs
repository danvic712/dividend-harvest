namespace DividendHarvest.Application.Exceptions;

public sealed class ModelParameterVersionAlreadyExistsException(
    string securityCode,
    DateOnly effectiveFromDate)
    : ApplicationExceptionBase(
        "model_parameter_version_already_exists",
        new Dictionary<string, object?>
        {
            ["securityCode"] = securityCode,
            ["effectiveFromDate"] = effectiveFromDate
        });
