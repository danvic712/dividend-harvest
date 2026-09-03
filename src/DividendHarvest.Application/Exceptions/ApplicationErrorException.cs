namespace DividendHarvest.Application.Exceptions;

public sealed class ApplicationErrorException(
    string errorCode,
    IReadOnlyDictionary<string, object?>? parameters = null,
    Exception? innerException = null)
    : ApplicationExceptionBase(
        errorCode,
        parameters ?? new Dictionary<string, object?>(),
        innerException);
