namespace DividendHarvest.Application.Exceptions;

public abstract class ApplicationExceptionBase(
    string errorCode,
    string message,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ErrorCode { get; } = errorCode;
}
