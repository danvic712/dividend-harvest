using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class FinancialSnapshotConfiguration
    : IEntityTypeConfiguration<FinancialSnapshot>
{
    public void Configure(EntityTypeBuilder<FinancialSnapshot> builder)
    {
        builder.ToTable("financial_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("financial_snapshot_id");
        builder.Property(x => x.SecurityId).HasColumnName("security_id");
        builder.Property(x => x.DataAsOfDate)
            .HasColumnName("data_as_of_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("TEXT");
        builder.Property(x => x.CapturedAt)
            .HasColumnName("captured_at")
            .HasColumnType("TEXT");
        builder.Property(x => x.EarningsPerShare)
            .HasColumnName("earnings_per_share")
            .HasPrecision(20, 8);
        builder.Property(x => x.DividendPayoutRatio)
            .HasColumnName("dividend_payout_ratio")
            .HasPrecision(20, 8);
        builder.Property(x => x.ThreeYearAverageDividendPayoutRatio)
            .HasColumnName("three_year_average_dividend_payout_ratio")
            .HasPrecision(20, 8);
        builder.Property(x => x.PriceToBookRatio)
            .HasColumnName("price_to_book_ratio")
            .HasPrecision(20, 8);
        builder.Property(x => x.ReturnOnEquity)
            .HasColumnName("return_on_equity")
            .HasPrecision(20, 8);
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
            x.DataAsOfDate
        }).IsUnique();

        builder.HasOne<Security>()
            .WithMany()
            .HasForeignKey(x => x.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
