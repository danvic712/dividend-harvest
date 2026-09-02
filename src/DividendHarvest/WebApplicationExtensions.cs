using DividendHarvest.Domain.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace DividendHarvest;

public static class WebApplicationExtensions
{
    public static WebApplication UseDividendHarvest(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.MapControllers();
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });
        app.MapHealthChecks("/readyz", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }

    public static async Task InitializeDividendHarvestDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUow>();
        await uow.EnsureCreatedAsync(cancellationToken);
    }
}
