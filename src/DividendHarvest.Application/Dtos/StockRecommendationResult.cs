namespace DividendHarvest.Application.Dtos;

public sealed record StockRecommendationResult(
    StockAnalysisResult Analysis,
    int SuggestedBuyShares,
    int SuggestedSellShares,
    decimal SuggestedTradeAmount,
    decimal EstimatedTransactionFeeAmount);
