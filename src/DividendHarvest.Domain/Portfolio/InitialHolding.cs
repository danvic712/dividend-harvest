namespace DividendHarvest.Domain.Portfolio;

public sealed record InitialHolding
{
    private InitialHolding(int heldShares, int coreShares, int targetShares, decimal averageCostPerShare)
    {
        HeldShares = heldShares;
        CoreShares = coreShares;
        TargetShares = targetShares;
        AverageCostPerShare = averageCostPerShare;
    }

    public int HeldShares { get; }

    public int CoreShares { get; }

    public int TargetShares { get; }

    public decimal AverageCostPerShare { get; }

    public static InitialHolding Create(
        int heldShares,
        int coreShares,
        int targetShares,
        decimal averageCostPerShare)
    {
        if (heldShares < 0)
        {
            throw new ArgumentException("持股数量不能为负数。", nameof(heldShares));
        }

        if (coreShares < 0 || coreShares > heldShares)
        {
            throw new ArgumentException("核心仓数量不能为负数或超过持股数量。", nameof(coreShares));
        }

        if (targetShares < 0)
        {
            throw new ArgumentException("目标股数不能为负数。", nameof(targetShares));
        }

        if (averageCostPerShare < 0)
        {
            throw new ArgumentException("平均成本不能为负数。", nameof(averageCostPerShare));
        }

        return new InitialHolding(heldShares, coreShares, targetShares, averageCostPerShare);
    }
}
