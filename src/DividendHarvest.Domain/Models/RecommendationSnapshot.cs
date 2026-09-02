namespace DividendHarvest.Domain.Models;

public sealed class RecommendationSnapshot
{
    private static readonly string[] SupportedModelStatusCodes =
        ["available", "cautious", "failed", "unavailable", "re_evaluate"];

    private static readonly string[] SupportedReliabilityCodes =
        ["passed", "cautious", "failed", "unavailable"];

    private RecommendationSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid ModelRunId { get; private set; }

    public Guid PortfolioId { get; private set; }

    public Guid SecurityId { get; private set; }

    public DateOnly? DataAsOfDate { get; private set; }

    public decimal? ClosePrice { get; private set; }

    public decimal? ModelDividendPerShare { get; private set; }

    public string? DividendModeCode { get; private set; }

    public string ModelStatusCode { get; private set; } = string.Empty;

    public string DividendReliabilityCode { get; private set; } = string.Empty;

    public string? PriceZoneCode { get; private set; }

    public string RecommendationCode { get; private set; } = string.Empty;

    public decimal? DividendYield { get; private set; }

    public int SuggestedBuyShares { get; private set; }

    public int SuggestedSellShares { get; private set; }

    public decimal SuggestedTradeAmount { get; private set; }

    public decimal EstimatedTransactionFeeAmount { get; private set; }

    public DateTimeOffset ComputedAt { get; private set; }

    public Guid? ModelParameterSetId { get; private set; }

    public static RecommendationSnapshot Create(
        Guid modelRunId,
        Guid portfolioId,
        Guid securityId,
        DateOnly? dataAsOfDate,
        decimal? closePrice,
        decimal? modelDividendPerShare,
        string? dividendModeCode,
        string modelStatusCode,
        string dividendReliabilityCode,
        string? priceZoneCode,
        string recommendationCode,
        decimal? dividendYield,
        int suggestedBuyShares,
        int suggestedSellShares,
        decimal suggestedTradeAmount,
        decimal estimatedTransactionFeeAmount,
        DateTimeOffset computedAt,
        Guid? modelParameterSetId)
    {
        if (modelRunId == Guid.Empty)
        {
            throw new ArgumentException("模型运行标识不能为空。", nameof(modelRunId));
        }

        if (portfolioId == Guid.Empty)
        {
            throw new ArgumentException("投资组合标识不能为空。", nameof(portfolioId));
        }

        if (securityId == Guid.Empty)
        {
            throw new ArgumentException("股票标识不能为空。", nameof(securityId));
        }

        ValidateOptionalPositive(closePrice, nameof(closePrice), "收盘价必须大于零。");
        ValidateOptionalPositive(
            modelDividendPerShare,
            nameof(modelDividendPerShare),
            "模型股息必须大于零。");
        ValidateOptionalRatio(dividendYield, nameof(dividendYield));

        var normalizedModelStatus = modelStatusCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!SupportedModelStatusCodes.Contains(normalizedModelStatus, StringComparer.Ordinal))
        {
            throw new ArgumentException("模型状态代码不受支持。", nameof(modelStatusCode));
        }

        var normalizedReliability =
            dividendReliabilityCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!SupportedReliabilityCodes.Contains(normalizedReliability, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "股息可靠性代码不受支持。",
                nameof(dividendReliabilityCode));
        }

        if (string.IsNullOrWhiteSpace(recommendationCode))
        {
            throw new ArgumentException("建议代码不能为空。", nameof(recommendationCode));
        }

        if (suggestedBuyShares < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(suggestedBuyShares),
                suggestedBuyShares,
                "建议买入股数不能为负数。");
        }

        if (suggestedSellShares < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(suggestedSellShares),
                suggestedSellShares,
                "建议卖出股数不能为负数。");
        }

        if (suggestedTradeAmount < 0 || estimatedTransactionFeeAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(suggestedTradeAmount),
                "建议交易金额和手续费不能为负数。");
        }

        if (computedAt == default)
        {
            throw new ArgumentException("计算时间不能为空。", nameof(computedAt));
        }

        return new RecommendationSnapshot
        {
            Id = Guid.NewGuid(),
            ModelRunId = modelRunId,
            PortfolioId = portfolioId,
            SecurityId = securityId,
            DataAsOfDate = dataAsOfDate,
            ClosePrice = closePrice,
            ModelDividendPerShare = modelDividendPerShare,
            DividendModeCode = NormalizeOptionalCode(dividendModeCode),
            ModelStatusCode = normalizedModelStatus,
            DividendReliabilityCode = normalizedReliability,
            PriceZoneCode = NormalizeOptionalCode(priceZoneCode),
            RecommendationCode = recommendationCode.Trim().ToLowerInvariant(),
            DividendYield = dividendYield,
            SuggestedBuyShares = suggestedBuyShares,
            SuggestedSellShares = suggestedSellShares,
            SuggestedTradeAmount = suggestedTradeAmount,
            EstimatedTransactionFeeAmount = estimatedTransactionFeeAmount,
            ComputedAt = computedAt.ToUniversalTime(),
            ModelParameterSetId = modelParameterSetId
        };
    }

    private static void ValidateOptionalPositive(
        decimal? value,
        string parameterName,
        string message)
    {
        if (value is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, message);
        }
    }

    private static void ValidateOptionalRatio(decimal? value, string parameterName)
    {
        if (value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "股息率必须介于 0 和 1 之间。");
        }
    }

    private static string? NormalizeOptionalCode(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
}
