using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure;

internal sealed class DividendHarvestDbContext(DbContextOptions<DividendHarvestDbContext> options)
    : DbContext(options)
{
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    public DbSet<Security> Securities => Set<Security>();

    public DbSet<PortfolioPosition> PortfolioPositions => Set<PortfolioPosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DividendHarvestDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
