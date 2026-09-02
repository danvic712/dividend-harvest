using DividendHarvest.Background;
using DividendHarvest.Configuration;
using DividendHarvest.ExceptionHandling;
using DividendHarvest.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DividendHarvest;

public static class HostServiceCollectionExtensions
{
    public static IServiceCollection AddDividendHarvestHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ApplicationExceptionHandler>();
        services.AddControllers();
        services.Configure<DailySyncOptions>(
            configuration.GetSection(DailySyncOptions.SectionName));
        services.AddHostedService<DailyStockDataSyncHostedService>();
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}
