using System.Linq.Expressions;

namespace DividendHarvest.Domain.Contracts;

/// <summary>
/// Provides persistence operations for one entity type without exposing the
/// underlying query provider or ORM implementation.
/// </summary>
public interface IRepository<TEntity>
    where TEntity : class
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true);

    Task<TEntity?> SingleOrDefaultAsync(
        CancellationToken cancellationToken = default,
        bool asNoTracking = true);

    Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        IReadOnlyList<Expression<Func<TEntity, object>>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);

    Task<int> RemoveWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}
