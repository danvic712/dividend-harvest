namespace DividendHarvest.Application.Exceptions;

public static class ApplicationErrors
{
    public static ApplicationValidationException Validation(
        string errorCode,
        string message)
        => new(errorCode, message);

    public static ApplicationErrorException Simple(
        string errorCode,
        Exception? innerException = null)
        => new(errorCode, innerException: innerException);

    public static ApplicationErrorException WithSecurity(
        string errorCode,
        string securityCode,
        Exception? innerException = null)
        => new(
            errorCode,
            new Dictionary<string, object?>
            {
                ["securityCode"] = securityCode
            },
            innerException);

    public static ApplicationErrorException WithSecurityReference(
        string errorCode,
        string securityCode,
        string exchangeCode,
        Exception? innerException = null)
        => new(
            errorCode,
            new Dictionary<string, object?>
            {
                ["securityCode"] = securityCode,
                ["exchangeCode"] = exchangeCode
            },
            innerException);

    public static ApplicationErrorException WithSourceRecord(
        string errorCode,
        string sourceRecordId)
        => new(
            errorCode,
            new Dictionary<string, object?>
            {
                ["sourceRecordId"] = sourceRecordId
            });

    public static ApplicationErrorException WithModelParameterVersion(
        string errorCode,
        string securityCode,
        DateOnly effectiveFromDate)
        => new(
            errorCode,
            new Dictionary<string, object?>
            {
                ["securityCode"] = securityCode,
                ["effectiveFromDate"] = effectiveFromDate
            });
}
