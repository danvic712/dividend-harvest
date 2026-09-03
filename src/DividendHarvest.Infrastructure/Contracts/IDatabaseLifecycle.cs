namespace DividendHarvest.Infrastructure.Contracts;

public interface IDatabaseLifecycle
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
