using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class PortfolioTradeConfiguration
    : IEntityTypeConfiguration<PortfolioTrade>
{
    public void Configure(EntityTypeBuilder<PortfolioTrade> builder)
    {
        builder.ToTable("portfolio_trades");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("portfolio_trade_id");
        builder.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(x => x.SecurityId).HasColumnName("security_id");
        builder.Property(x => x.TradeDate)
            .HasColumnName("trade_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.TradeDirectionCode)
            .HasColumnName("trade_direction_code")
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(x => x.ShareQuantity).HasColumnName("share_quantity");
        builder.Property(x => x.PricePerShare)
            .HasColumnName("price_per_share")
            .HasPrecision(20, 8)
            .IsRequired();
        builder.Property(x => x.TransactionFeeAmount)
            .HasColumnName("transaction_fee_amount")
            .HasPrecision(20, 8)
            .IsRequired();
        builder.Property(x => x.SourceRecordId)
            .HasColumnName("source_record_id")
            .HasMaxLength(200);

        builder.HasIndex(x => new
        {
            x.PortfolioId,
            x.TradeDate
        });
        builder.HasIndex(x => new
        {
            x.PortfolioId,
            x.SourceRecordId
        });

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
