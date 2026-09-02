using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure;

internal sealed class DividendHarvestDbContext(DbContextOptions<DividendHarvestDbContext> options)
    : DbContext(options)
{
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    public DbSet<Security> Securities => Set<Security>();

    public DbSet<PortfolioPosition> PortfolioPositions => Set<PortfolioPosition>();

    public DbSet<ModelParameterSet> ModelParameterSets => Set<ModelParameterSet>();

    public DbSet<PriceObservation> PriceObservations => Set<PriceObservation>();

    public DbSet<DividendEvent> DividendEvents => Set<DividendEvent>();

    public DbSet<FinancialSnapshot> FinancialSnapshots => Set<FinancialSnapshot>();

    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();

    public DbSet<RecommendationSnapshot> RecommendationSnapshots => Set<RecommendationSnapshot>();

    public DbSet<PortfolioTrade> PortfolioTrades => Set<PortfolioTrade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DividendHarvestDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
