using DividendHarvest.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DividendHarvest.Infrastructure.Configurations;

public sealed class RecommendationSnapshotConfiguration
    : IEntityTypeConfiguration<RecommendationSnapshot>
{
    public void Configure(EntityTypeBuilder<RecommendationSnapshot> builder)
    {
        builder.ToTable("recommendation_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("recommendation_snapshot_id");
        builder.Property(x => x.ModelRunId).HasColumnName("model_run_id");
        builder.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(x => x.SecurityId).HasColumnName("security_id");
        builder.Property(x => x.DataAsOfDate)
            .HasColumnName("data_as_of_date")
            .HasColumnType("TEXT");
        builder.Property(x => x.ClosePrice)
            .HasColumnName("close_price")
            .HasPrecision(20, 8);
        builder.Property(x => x.ModelDividendPerShare)
            .HasColumnName("model_dividend_per_share")
            .HasPrecision(20, 8);
        builder.Property(x => x.DividendModeCode)
            .HasColumnName("dividend_mode_code")
            .HasMaxLength(32);
        builder.Property(x => x.ModelStatusCode)
            .HasColumnName("model_status_code")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.DividendReliabilityCode)
            .HasColumnName("dividend_reliability_code")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.PriceZoneCode)
            .HasColumnName("price_zone_code")
            .HasMaxLength(32);
        builder.Property(x => x.RecommendationCode)
            .HasColumnName("recommendation_code")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.DividendYield)
            .HasColumnName("dividend_yield")
            .HasPrecision(20, 8);
        builder.Property(x => x.SuggestedBuyShares).HasColumnName("suggested_buy_shares");
        builder.Property(x => x.SuggestedSellShares).HasColumnName("suggested_sell_shares");
        builder.Property(x => x.SuggestedTradeAmount)
            .HasColumnName("suggested_trade_amount")
            .HasPrecision(20, 8)
            .IsRequired();
        builder.Property(x => x.EstimatedTransactionFeeAmount)
            .HasColumnName("estimated_transaction_fee_amount")
            .HasPrecision(20, 8)
            .IsRequired();
        builder.Property(x => x.ComputedAt)
            .HasColumnName("computed_at")
            .HasColumnType("TEXT")
            .IsRequired();
        builder.Property(x => x.ModelParameterSetId)
            .HasColumnName("model_parameter_set_id");

        builder.HasIndex(x => new
        {
            x.ModelRunId,
            x.PortfolioId,
            x.SecurityId
        }).IsUnique();

        builder.HasOne<Portfolio>()
            .WithMany()
            .HasForeignKey(x => x.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Security>()
            .WithMany()
            .HasForeignKey(x => x.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ModelParameterSet>()
            .WithMany()
            .HasForeignKey(x => x.ModelParameterSetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
