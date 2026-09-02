using System.Reflection;

namespace DividendHarvest.Application.Exceptions;

public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(
        Exception? innerException = null)
        : this(new Dictionary<string, object?>(), innerException)
    {
    }

    protected ApplicationExceptionBase(
        IReadOnlyDictionary<string, object?> parameters,
        Exception? innerException = null)
        : base(null, innerException)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var errorCodeAttribute = GetType().GetCustomAttribute<ApplicationErrorCodeAttribute>()
            ?? throw new InvalidOperationException(
                $"Application exception '{GetType().FullName}' must declare {nameof(ApplicationErrorCodeAttribute)}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCodeAttribute.ErrorCode);

        ErrorCode = errorCodeAttribute.ErrorCode;
        Parameters = parameters.ToDictionary(
            parameter => parameter.Key,
            parameter => parameter.Value,
            StringComparer.Ordinal);
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, object?> Parameters { get; }

    public override string Message => ErrorCode;
}
