using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure.DataAccess;

internal sealed class DividendHarvestDbContext(DbContextOptions<DividendHarvestDbContext> options)
    : DbContext(options)
{
    public DbSet<PortfolioEntity> Portfolios => Set<PortfolioEntity>();

    public DbSet<SecurityEntity> Securities => Set<SecurityEntity>();

    public DbSet<PortfolioPositionEntity> PortfolioPositions => Set<PortfolioPositionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DividendHarvestDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
