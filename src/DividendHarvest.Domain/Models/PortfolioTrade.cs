using DividendHarvest.Domain.Portfolio;

namespace DividendHarvest.Domain.Models;

public sealed class PortfolioTrade
{
    private PortfolioTrade()
    {
    }

    public Guid Id { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Guid SecurityId { get; private set; }

    public DateOnly TradeDate { get; private set; }

    public string TradeDirectionCode { get; private set; } = string.Empty;

    public int ShareQuantity { get; private set; }

    public decimal PricePerShare { get; private set; }

    public decimal TransactionFeeAmount { get; private set; }

    public string? SourceRecordId { get; private set; }

    public static PortfolioTrade Create(
        Guid portfolioId,
        Guid securityId,
        DateOnly tradeDate,
        string tradeDirectionCode,
        int shareQuantity,
        decimal pricePerShare,
        decimal transactionFeeAmount,
        string? sourceRecordId)
    {
        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException("投资组合标识不能为空。", nameof(portfolioId));
        }

        if (securityId == Guid.Empty)
        {
            throw new ArgumentException("股票标识不能为空。", nameof(securityId));
        }

        if (tradeDate == DateOnly.MinValue)
        {
            throw new ArgumentException("交易日期不能为空。", nameof(tradeDate));
        }

        var normalizedDirection =
            tradeDirectionCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!TradeDirectionCodes.IsSupported(normalizedDirection))
        {
            throw new ArgumentException("交易方向必须是 buy 或 sell。", nameof(tradeDirectionCode));
        }

        if (shareQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shareQuantity),
                shareQuantity,
                "交易股数必须大于零。");
        }

        if (pricePerShare <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pricePerShare),
                pricePerShare,
                "成交价格必须大于零。");
        }

        if (transactionFeeAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transactionFeeAmount),
                transactionFeeAmount,
                "交易费用不能为负数。");
        }

        return new PortfolioTrade
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            SecurityId = securityId,
            TradeDate = tradeDate,
            TradeDirectionCode = normalizedDirection,
            ShareQuantity = shareQuantity,
            PricePerShare = pricePerShare,
            TransactionFeeAmount = transactionFeeAmount,
            SourceRecordId = string.IsNullOrWhiteSpace(sourceRecordId)
                ? null
                : sourceRecordId.Trim()
        };
    }
}
