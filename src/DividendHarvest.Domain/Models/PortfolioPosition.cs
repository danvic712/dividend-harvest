namespace DividendHarvest.Domain.Models;

public sealed class PortfolioPosition
{
    public Guid PortfolioId { get; set; }

    public Guid SecurityId { get; set; }

    public int HeldShares { get; set; }

    public int CoreShares { get; set; }

    public int TargetShares { get; set; }

    public decimal AverageCostPerShare { get; set; }

    public void ApplyBuy(
        int shareQuantity,
        decimal pricePerShare,
        decimal transactionFeeAmount)
    {
        if (shareQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shareQuantity),
                shareQuantity,
                "买入股数必须大于零。");
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

        var existingCost = HeldShares * AverageCostPerShare;
        HeldShares += shareQuantity;
        AverageCostPerShare =
            (existingCost + shareQuantity * pricePerShare + transactionFeeAmount)
            / HeldShares;
    }

    public void ApplySell(int shareQuantity)
    {
        if (shareQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shareQuantity),
                shareQuantity,
                "卖出股数必须大于零。");
        }

        if (shareQuantity > HeldShares)
        {
            throw new InvalidOperationException("卖出股数不能超过当前持股数量。");
        }

        HeldShares -= shareQuantity;
        if (HeldShares == 0)
        {
            AverageCostPerShare = 0m;
        }
    }
}
