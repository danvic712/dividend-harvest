using DividendHarvest.Domain.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DividendHarvest.HealthChecks;

public sealed class DatabaseHealthCheck(IUow uow) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await uow.CanConnectAsync(cancellationToken)
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
