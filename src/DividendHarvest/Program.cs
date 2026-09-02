using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Setup;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Infrastructure;
using DividendHarvest.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddScoped<ISetupAppService, SetupAppService>();
builder.Services.AddDividendHarvestDataAccess(builder.Configuration);
builder.Services.AddFtShareStockDataProvider(builder.Configuration);

var app = builder.Build();

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

await InitializeDatabaseAsync(app.Services);

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var uow = scope.ServiceProvider.GetRequiredService<IUow>();
    await uow.EnsureCreatedAsync(CancellationToken.None);
}
