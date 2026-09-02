using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class PortfolioPositionConfiguration
    : IEntityTypeConfiguration<PortfolioPosition>
{
    public void Configure(EntityTypeBuilder<PortfolioPosition> builder)
    {
        builder.ToTable("portfolio_positions");
        builder.HasKey(x => new { x.PortfolioId, x.SecurityId });
        builder.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(x => x.SecurityId).HasColumnName("security_id");
        builder.Property(x => x.HeldShares).HasColumnName("held_shares");
        builder.Property(x => x.CoreShares).HasColumnName("core_shares");
        builder.Property(x => x.TargetShares).HasColumnName("target_shares");
        builder.Property(x => x.AverageCostPerShare)
            .HasColumnName("average_cost_per_share")
            .HasPrecision(20, 8);
        builder.HasOne<Portfolio>()
            .WithMany()
            .HasForeignKey(x => x.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Security>()
            .WithMany()
            .HasForeignKey(x => x.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
