using DividendHarvest.Domain.Contracts;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DividendHarvest;

public static class WebApplicationExtensions
{
    public static WebApplication UseDividendHarvest(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in app.DescribeApiVersions())
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"Dividend Harvest API {description.GroupName}");
            }

            options.RoutePrefix = "swagger";
        });
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
