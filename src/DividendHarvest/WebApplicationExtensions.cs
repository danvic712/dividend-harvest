using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Diagnostics;
using DividendHarvest.Infrastructure.Contracts;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ApplicationDiagnosticContext = DividendHarvest.Application.Contracts.IDiagnosticContext;

namespace DividendHarvest;

public static class WebApplicationExtensions
{
    public static WebApplication UseDividendHarvest(this WebApplication app)
    {
        app.UseDividendHarvestDiagnosticContext();
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

    private static IApplicationBuilder UseDividendHarvestDiagnosticContext(
        this IApplicationBuilder app)
    {
        app.Use(async (httpContext, next) =>
        {
            var diagnosticContext = httpContext.RequestServices
                .GetRequiredService<ApplicationDiagnosticContext>();
            var correlationId = httpContext.TraceIdentifier;
            httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

            using var diagnosticScope = diagnosticContext.BeginScope(new DiagnosticScope(
                "http_request",
                CorrelationId: correlationId));
            await next();
        });

        return app;
    }

    public static async Task InitializeDividendHarvestDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var databaseLifecycle = scope.ServiceProvider
            .GetRequiredService<IDatabaseLifecycle>();
        await databaseLifecycle.EnsureCreatedAsync(cancellationToken);
    }

    public static async Task RunDividendHarvestAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        app.UseDividendHarvest();
        await app.InitializeDividendHarvestDatabaseAsync(cancellationToken);
        await app.RunAsync(cancellationToken);
    }
}
