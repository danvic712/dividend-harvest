using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Portfolio;
using DividendHarvest.Application.DividendStrategy;
using DividendHarvest.Application.Setup;
using DividendHarvest.Application.Stocks;
using DividendHarvest.Application.Validators;
using DividendHarvest.Background;
using DividendHarvest.Configuration;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.ExceptionHandling;
using DividendHarvest.Infrastructure;
using DividendHarvest.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<SetupRequestValidator>();
builder.Services.AddScoped<IStockWatchlistAppService, StockWatchlistAppService>();
builder.Services.AddScoped<IStockModelParameterAppService, StockModelParameterAppService>();
builder.Services.AddScoped<
    IStockPriceObservationAppService,
    StockPriceObservationAppService>();
builder.Services.AddScoped<
    IStockDividendEventAppService,
    StockDividendEventAppService>();
builder.Services.AddScoped<
    IStockFinancialSnapshotAppService,
    StockFinancialSnapshotAppService>();
builder.Services.AddScoped<IStockAnalysisAppService, StockAnalysisAppService>();
builder.Services.AddScoped<IBudgetAppService, BudgetAppService>();
builder.Services.AddScoped<
    IPortfolioRecommendationAppService,
    PortfolioRecommendationAppService>();
builder.Services.AddScoped<
    IRecommendationSnapshotAppService,
    RecommendationSnapshotAppService>();
builder.Services.AddScoped<IPortfolioTradeAppService, PortfolioTradeAppService>();
builder.Services.AddScoped<
    IStockDailyDataSyncAppService,
    StockDailyDataSyncAppService>();
builder.Services.Configure<DailySyncOptions>(
    builder.Configuration.GetSection(DailySyncOptions.SectionName));
builder.Services.AddHostedService<DailyStockDataSyncHostedService>();
builder.Services.AddSingleton(TimeProvider.System);
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
