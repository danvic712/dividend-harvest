namespace DividendHarvest.Application.Exceptions;

public abstract class ApplicationValidationException(
    string errorCode,
    string message)
    : ApplicationExceptionBase(errorCode, message);
