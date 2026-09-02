using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class DividendEventConfiguration
    : IEntityTypeConfiguration<DividendEvent>
{
    public void Configure(EntityTypeBuilder<DividendEvent> builder)
    {
        builder.ToTable("dividend_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("dividend_event_id");
        builder.Property(x => x.SecurityId).HasColumnName("security_id");
        builder.Property(x => x.DividendPerShare)
            .HasColumnName("dividend_per_share")
            .HasPrecision(20, 8);
        builder.Property(x => x.DividendTypeCode)
            .HasColumnName("dividend_type_code")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.DividendStatusCode)
            .HasColumnName("dividend_status_code")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.AnnouncementDate)
            .HasColumnName("announcement_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.ExDividendDate)
            .HasColumnName("ex_dividend_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.PaymentDate)
            .HasColumnName("payment_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.IsSpecialDividend)
            .HasColumnName("is_special_dividend")
            .IsRequired();
        builder.Property(x => x.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("TEXT");
        builder.Property(x => x.CapturedAt)
            .HasColumnName("captured_at")
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
            x.SourceRecordId
        }).IsUnique();

        builder.HasOne<Security>()
            .WithMany()
            .HasForeignKey(x => x.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
