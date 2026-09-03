using DividendHarvest.Infrastructure.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DividendHarvest.HealthChecks;

public sealed class DatabaseHealthCheck(IServiceScopeFactory serviceScopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var databaseLifecycle = scope.ServiceProvider
                .GetRequiredService<IDatabaseLifecycle>();
            return await databaseLifecycle.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("数据库不可用。");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("数据库检查失败。", exception);
        }
    }
}
