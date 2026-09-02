namespace DividendHarvest.Application.Exceptions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ApplicationErrorCodeAttribute(string errorCode) : Attribute
{
    public string ErrorCode { get; } = errorCode;
}
