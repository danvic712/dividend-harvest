namespace DividendHarvest.Application.Exceptions;

public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(
        string errorCode,
        Exception? innerException = null)
        : this(errorCode, new Dictionary<string, object?>(), innerException)
    {
    }

    protected ApplicationExceptionBase(
        string errorCode,
        IReadOnlyDictionary<string, object?> parameters,
        Exception? innerException = null)
        : base(errorCode, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentNullException.ThrowIfNull(parameters);

        ErrorCode = errorCode;
        Parameters = parameters.ToDictionary(
            parameter => parameter.Key,
            parameter => parameter.Value,
            StringComparer.Ordinal);
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, object?> Parameters { get; }
}
