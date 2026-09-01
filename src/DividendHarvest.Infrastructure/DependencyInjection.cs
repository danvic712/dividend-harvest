using DividendHarvest.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DividendHarvest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDividendHarvestDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=dividend-harvest.db";

        services.AddDbContext<DataAccess.DividendHarvestDbContext>(options =>
            options.UseSqlite(connectionString));
        services.AddScoped<ISetupRepository, DataAccess.EfSetupRepository>();
        services.AddScoped<IUnitOfWork, DataAccess.EfUnitOfWork>();

        return services;
    }

    public static IServiceCollection AddFtShareStockDataProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<FtShare.FtShareOptions>()
            .Bind(configuration.GetSection(FtShare.FtShareOptions.SectionName));
        services.AddScoped<FtShare.IFtShareMcpToolInvoker, FtShare.FtShareMcpToolInvoker>();
        services.AddScoped<IStockDataProvider, FtShare.FtShareStockDataProvider>();

        return services;
    }
}
