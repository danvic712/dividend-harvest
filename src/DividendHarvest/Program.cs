using DividendHarvest;

var builder = WebApplication.CreateBuilder(args);
builder.AddDividendHarvest();

var app = builder.Build();

await app.RunDividendHarvestAsync();
