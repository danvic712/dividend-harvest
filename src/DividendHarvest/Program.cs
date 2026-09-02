using DividendHarvest;
using DividendHarvest.Application;
using DividendHarvest.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDividendHarvestApplication()
    .AddDividendHarvestInfrastructure(builder.Configuration)
    .AddDividendHarvestHost(builder.Configuration);

var app = builder.Build();

app.UseDividendHarvest();
await app.InitializeDividendHarvestDatabaseAsync();

await app.RunAsync();
