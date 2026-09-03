using System.Linq.Expressions;
using DividendHarvest.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure.Repositories;

internal sealed class EFRepository<TEntity>(DbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => CreateQuery(asNoTracking: true).AnyAsync(cancellationToken);

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => CreateQuery(asNoTracking: true).AnyAsync(predicate, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = CreateQuery(asNoTracking).Where(predicate);
        if (orderBy is not null)
        {
            query = descending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TEntity?> SingleOrDefaultAsync(
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
        => CreateQuery(asNoTracking).SingleOrDefaultAsync(cancellationToken);

    public Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
        => CreateQuery(asNoTracking).SingleOrDefaultAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        IReadOnlyList<Expression<Func<TEntity, object>>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = CreateQuery(asNoTracking);
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        if (orderBy is not null && orderBy.Count > 0)
        {
            var orderedQuery = descending
                ? query.OrderByDescending(orderBy[0])
                : query.OrderBy(orderBy[0]);
            for (var index = 1; index < orderBy.Count; index++)
            {
                orderedQuery = descending
                    ? orderedQuery.ThenByDescending(orderBy[index])
                    : orderedQuery.ThenBy(orderBy[index]);
            }

            query = orderedQuery;
        }

        return await query.ToListAsync(cancellationToken);
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

    private IQueryable<TEntity> CreateQuery(bool asNoTracking)
    {
        var query = dbContext.Set<TEntity>().AsQueryable();
        return asNoTracking ? query.AsNoTracking() : query;
    }
}
