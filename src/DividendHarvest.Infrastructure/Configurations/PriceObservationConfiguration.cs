using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class PriceObservationConfiguration
    : IEntityTypeConfiguration<PriceObservation>
{
    public void Configure(EntityTypeBuilder<PriceObservation> builder)
    {
        builder.ToTable("price_observations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("price_observation_id");
        builder.Property(x => x.SecurityId).HasColumnName("security_id");
        builder.Property(x => x.TradingDate)
            .HasColumnName("trading_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.ClosePrice)
            .HasColumnName("close_price")
            .HasPrecision(20, 8);
        builder.Property(x => x.PriceObservedAt)
            .HasColumnName("price_observed_at")
            .HasColumnType("TEXT");
        builder.Property(x => x.DataSource)
            .HasColumnName("data_source")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.SourceRecordId)
            .HasColumnName("source_record_id")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(x => x.DataQualityCode)
            .HasColumnName("data_quality_code")
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SecurityId,
            x.TradingDate
        }).IsUnique();

        builder.HasOne<Security>()
            .WithMany()
            .HasForeignKey(x => x.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
