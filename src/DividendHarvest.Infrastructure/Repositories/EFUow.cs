using System.Collections.Concurrent;
using DividendHarvest.Domain.Contracts;
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
        => dbContext.SaveChangesAsync(cancellationToken);

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        => dbContext.Database.CanConnectAsync(cancellationToken);

    public Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        => dbContext.Database.EnsureCreatedAsync(cancellationToken);
}
