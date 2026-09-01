using DividendHarvest.Application.Ports;
using DividendHarvest.Application.Setup;
using DividendHarvest.Infrastructure;
using DividendHarvest.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddScoped<ISetupAppService, SetupAppService>();
builder.Services.AddDividendHarvestDataAccess(builder.Configuration);
builder.Services.AddFtShareStockDataProvider(builder.Configuration);

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

app.MapPost("/api/setup", async (
    SetupRequest request,
    ISetupAppService setupAppService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await setupAppService.InitializeAsync(request, cancellationToken);
        return Results.Created("/api/setup/status", result);
    }
    catch (SetupValidationException exception)
    {
        return Results.Problem(
            detail: exception.Message,
            statusCode: StatusCodes.Status400BadRequest,
            title: "建账请求无效");
    }
    catch (SetupAlreadyCompletedException exception)
    {
        return Results.Problem(
            detail: exception.Message,
            statusCode: StatusCodes.Status409Conflict,
            title: "系统已经完成建账");
    }
    catch (StockDataUnavailableException exception)
    {
        return Results.Problem(
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "股票基础资料不可用");
    }
});

await InitializeDatabaseAsync(app.Services);

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DividendHarvestDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
