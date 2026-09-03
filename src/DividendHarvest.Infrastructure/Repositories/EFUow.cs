using System.Collections.Concurrent;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure.Repositories;

internal sealed class EFUow(DividendHarvestDbContext dbContext) : IUow
{
    private readonly ConcurrentDictionary<Type, Lazy<object>> repositories = new();

    public IRepository<TEntity> Get<TEntity>()
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        var repository = repositories.GetOrAdd(
            entityType,
            _ => new Lazy<object>(
                () => new EFRepository<TEntity>(dbContext),
                isThreadSafe: true));

        return (IRepository<TEntity>)repository.Value;
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => CommitChangesAsync(cancellationToken);

    private async Task<int> CommitChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new UnitOfWorkCommitException(
                exception,
                IsUniqueConstraintViolation(exception));
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.GetBaseException() is SqliteException
        {
            SqliteExtendedErrorCode: 2067
        };

}
