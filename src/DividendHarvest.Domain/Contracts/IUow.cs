namespace DividendHarvest.Domain.Contracts;

public interface IUow
{
    IRepository<TEntity> Get<TEntity>()
        where TEntity : class;

    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
