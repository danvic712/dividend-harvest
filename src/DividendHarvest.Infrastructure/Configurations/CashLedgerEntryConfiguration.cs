using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class CashLedgerEntryConfiguration
    : IEntityTypeConfiguration<CashLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CashLedgerEntry> builder)
    {
        builder.ToTable("cash_ledger_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("cash_ledger_entry_id");
        builder.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(x => x.SecurityId).HasColumnName("security_id");
        builder.Property(x => x.EntryDate)
            .HasColumnName("entry_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.EntryTypeCode)
            .HasColumnName("entry_type_code")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.CashDirectionCode)
            .HasColumnName("cash_direction_code")
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(x => x.CashAmount)
            .HasColumnName("cash_amount")
            .HasPrecision(20, 8)
            .IsRequired();
        builder.Property(x => x.SourceRecordId)
            .HasColumnName("source_record_id")
            .HasMaxLength(200);

        builder.HasIndex(x => new
        {
            x.PortfolioId,
            x.SourceRecordId
        }).HasFilter("source_record_id IS NOT NULL").IsUnique();

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
