using DividendHarvest.Infrastructure.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DividendHarvest.Infrastructure.DataAccess;

public sealed class DividendHarvestDbContext(DbContextOptions<DividendHarvestDbContext> options)
    : DbContext(options)
{
    public DbSet<PortfolioEntity> Portfolios => Set<PortfolioEntity>();

    public DbSet<SecurityEntity> Securities => Set<SecurityEntity>();

    public DbSet<PortfolioPositionEntity> PortfolioPositions => Set<PortfolioPositionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortfolioEntity>(entity =>
        {
            entity.ToTable("portfolios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("portfolio_id");
            entity.Property(x => x.Name).HasColumnName("portfolio_name").HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<SecurityEntity>(entity =>
        {
            entity.ToTable("securities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("security_id");
            entity.Property(x => x.SecurityCode).HasColumnName("security_code").HasMaxLength(6).IsRequired();
            entity.Property(x => x.ExchangeCode).HasColumnName("exchange_code").HasMaxLength(4).IsRequired();
            entity.Property(x => x.SecurityName).HasColumnName("security_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.MarketCode).HasColumnName("market_code").HasMaxLength(32).IsRequired();
            entity.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            entity.HasIndex(x => new { x.ExchangeCode, x.SecurityCode }).IsUnique();
        });

        modelBuilder.Entity<PortfolioPositionEntity>(entity =>
        {
            entity.ToTable("portfolio_positions");
            entity.HasKey(x => new { x.PortfolioId, x.SecurityId });
            entity.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
            entity.Property(x => x.SecurityId).HasColumnName("security_id");
            entity.Property(x => x.HeldShares).HasColumnName("held_shares");
            entity.Property(x => x.CoreShares).HasColumnName("core_shares");
            entity.Property(x => x.TargetShares).HasColumnName("target_shares");
            entity.Property(x => x.AverageCostPerShare).HasColumnName("average_cost_per_share").HasPrecision(20, 8);
            entity.HasOne<PortfolioEntity>()
                .WithMany()
                .HasForeignKey(x => x.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<SecurityEntity>()
                .WithMany()
                .HasForeignKey(x => x.SecurityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
