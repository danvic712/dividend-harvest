using DividendHarvest.Application.Contracts;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Infrastructure.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DividendHarvest.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDividendHarvestInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=dividend-harvest.db";

        services.AddDbContext<DividendHarvestDbContext>(options =>
            options.UseSqlite(connectionString));
        services.AddScoped<IUow, Repositories.EFUow>();

        services
            .AddOptions<FtShare.FtShareOptions>()
            .Bind(configuration.GetSection(FtShare.FtShareOptions.SectionName));
        services.AddScoped<IFtShareMcpToolInvoker, FtShare.FtShareMcpToolInvoker>();
        services.AddScoped<IStockDataProvider, FtShare.FtShareStockDataProvider>();

        return services;
    }
}
