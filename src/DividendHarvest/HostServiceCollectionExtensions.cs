using DividendHarvest.Application;
using DividendHarvest.Background;
using DividendHarvest.Configuration;
using DividendHarvest.ExceptionHandling;
using DividendHarvest.HealthChecks;
using DividendHarvest.Contracts;
using DividendHarvest.Diagnostics;
using Asp.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using DividendHarvest.Infrastructure;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using ApplicationDiagnosticContext = DividendHarvest.Application.Contracts.IDiagnosticContext;

namespace DividendHarvest;

public static class HostServiceCollectionExtensions
{
    public static WebApplicationBuilder AddDividendHarvest(
        this WebApplicationBuilder builder)
    {
        builder.Services
            .AddDividendHarvestApplication()
            .AddDividendHarvestInfrastructure(builder.Configuration)
            .AddDividendHarvestHost(builder.Configuration);
        return builder;
    }

    public static IServiceCollection AddDividendHarvestHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSerilog((serviceProvider, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext());
        services.AddProblemDetails();
        services.AddSingleton<ApplicationDiagnosticContext, SerilogDiagnosticContext>();
        services.AddSingleton<IHttpErrorRenderer, ProblemDetailsErrorRenderer>();
        services.AddExceptionHandler<ApplicationExceptionHandler>();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
        services.Configure<DailySyncOptions>(
            configuration.GetSection(DailySyncOptions.SectionName));
        services.AddSingleton<IDailyStockDataSyncRunner, DailyStockDataSyncRunner>();
        services.AddHostedService<DailyStockDataSyncHostedService>();
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}
