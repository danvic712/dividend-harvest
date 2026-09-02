using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> builder)
    {
        builder.ToTable("portfolios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("portfolio_id");
        builder.Property(x => x.Name)
            .HasColumnName("portfolio_name")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();
    }
}
