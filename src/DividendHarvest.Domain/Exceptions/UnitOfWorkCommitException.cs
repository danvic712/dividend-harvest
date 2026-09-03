namespace DividendHarvest.Domain.Exceptions;

/// <summary>
/// Represents a persistence commit failure without exposing the ORM provider
/// to an Application module.
/// </summary>
public sealed class UnitOfWorkCommitException(
    Exception innerException,
    bool isUniqueConstraintViolation = false)
    : Exception("The unit of work could not be committed.", innerException)
{
    public bool IsUniqueConstraintViolation { get; } = isUniqueConstraintViolation;
}
