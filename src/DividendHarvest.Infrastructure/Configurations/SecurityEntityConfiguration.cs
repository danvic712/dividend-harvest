using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class SecurityEntityConfiguration : IEntityTypeConfiguration<SecurityEntity>
{
    public void Configure(EntityTypeBuilder<SecurityEntity> builder)
    {
        builder.ToTable("securities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("security_id");
        builder.Property(x => x.SecurityCode)
            .HasColumnName("security_code")
            .HasMaxLength(6)
            .IsRequired();
        builder.Property(x => x.ExchangeCode)
            .HasColumnName("exchange_code")
            .HasMaxLength(4)
            .IsRequired();
        builder.Property(x => x.SecurityName)
            .HasColumnName("security_name")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(x => x.MarketCode)
            .HasColumnName("market_code")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();
        builder.HasIndex(x => new { x.ExchangeCode, x.SecurityCode }).IsUnique();
    }
}
