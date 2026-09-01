using DividendHarvest.Application.Ports;
using DividendHarvest.Application.Setup;
using DividendHarvest.Infrastructure;
using DividendHarvest.Infrastructure.DataAccess;
using DividendHarvest.Infrastructure.FtShare;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddScoped<ISetupAppService, SetupAppService>();
builder.Services.AddScoped<IStockDataProvider, PendingStockDataProvider>();
builder.Services.AddDividendHarvestDataAccess(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapGet("/readyz", async (DividendHarvestDbContext dbContext, CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/api/setup/status", async (
    ISetupAppService setupAppService,
    CancellationToken cancellationToken) =>
{
    var status = await setupAppService.GetStatusAsync(cancellationToken);
    return Results.Ok(status);
});

await InitializeDatabaseAsync(app.Services);

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DividendHarvestDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
