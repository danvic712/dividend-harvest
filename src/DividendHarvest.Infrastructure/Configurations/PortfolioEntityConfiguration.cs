using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class PortfolioEntityConfiguration : IEntityTypeConfiguration<PortfolioEntity>
{
    public void Configure(EntityTypeBuilder<PortfolioEntity> builder)
    {
        builder.ToTable("portfolios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("portfolio_id");
        builder.Property(x => x.Name)
            .HasColumnName("portfolio_name")
            .HasMaxLength(100)
            .IsRequired();
    }
}
