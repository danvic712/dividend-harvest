namespace DividendHarvest.Domain.Contracts;

public interface IUow
{
    IRepository<TEntity> Get<TEntity>()
        where TEntity : class;

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
