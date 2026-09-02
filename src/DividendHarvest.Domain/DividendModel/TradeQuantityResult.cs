namespace DividendHarvest.Domain.DividendModel;

public sealed record TradeQuantityResult(
    int SuggestedBuyShares,
    int SuggestedSellShares,
    decimal SuggestedTradeAmount,
    decimal EstimatedTransactionFeeAmount);
