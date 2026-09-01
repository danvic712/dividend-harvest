using System.Linq.Expressions;
using DividendHarvest.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure.DataAccess;

internal sealed class EFRepository<TEntity>(DbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    public IQueryable<TEntity> GetQueryable(
        bool asNoTracking = false,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = dbContext.Set<TEntity>();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return query;
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Remove(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
    }

    public Task<int> RemoveWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => dbContext.Set<TEntity>()
            .Where(predicate)
            .ExecuteDeleteAsync(cancellationToken);
}
