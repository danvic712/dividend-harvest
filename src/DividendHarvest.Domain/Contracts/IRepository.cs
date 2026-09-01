using System.Linq.Expressions;

namespace DividendHarvest.Domain.Contracts;

public interface IRepository<TEntity>
    where TEntity : class
{
    IQueryable<TEntity> GetQueryable(
        bool asNoTracking = false,
        params Expression<Func<TEntity, object>>[] includes);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);

    Task<int> RemoveWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}
