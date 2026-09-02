using DividendHarvest.Domain.Codes;

namespace DividendHarvest.Domain.Models;

public sealed class RecommendationSnapshot
{
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

    public string? ObservedPriceZoneCode { get; private set; }

    public string? PriceZoneCode { get; private set; }

    public bool PriceZoneConfirmed { get; private set; }

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
        string? observedPriceZoneCode,
        string? priceZoneCode,
        bool priceZoneConfirmed,
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
        if (!ModelStatusCodes.IsSupported(normalizedModelStatus))
        {
            throw new ArgumentException("模型状态代码不受支持。", nameof(modelStatusCode));
        }

        var normalizedReliability =
            dividendReliabilityCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!DividendReliabilityCodes.IsSupported(normalizedReliability))
        {
            throw new ArgumentException(
                "股息可靠性代码不受支持。",
                nameof(dividendReliabilityCode));
        }

        var normalizedDividendMode = NormalizeOptionalCode(dividendModeCode);
        if (normalizedDividendMode is not null
            && !DividendModeCodes.IsSupported(normalizedDividendMode))
        {
            throw new ArgumentException("股息模式代码不受支持。", nameof(dividendModeCode));
        }

        var normalizedObservedPriceZone = NormalizeOptionalCode(observedPriceZoneCode);
        if (normalizedObservedPriceZone is not null
            && !PriceZoneCodes.IsSupported(normalizedObservedPriceZone))
        {
            throw new ArgumentException(
                "观测价格区域代码不受支持。",
                nameof(observedPriceZoneCode));
        }

        var normalizedPriceZone = NormalizeOptionalCode(priceZoneCode);
        if (normalizedPriceZone is not null && !PriceZoneCodes.IsSupported(normalizedPriceZone))
        {
            throw new ArgumentException("确认价格区域代码不受支持。", nameof(priceZoneCode));
        }

        var normalizedRecommendation = recommendationCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!RecommendationCodes.IsSupported(normalizedRecommendation))
        {
            throw new ArgumentException("建议代码不受支持。", nameof(recommendationCode));
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

        ValidateSnapshotSemantics(
            normalizedDividendMode,
            modelDividendPerShare,
            normalizedModelStatus,
            normalizedReliability,
            normalizedObservedPriceZone,
            normalizedPriceZone,
            priceZoneConfirmed,
            normalizedRecommendation,
            closePrice,
            suggestedBuyShares,
            suggestedSellShares,
            suggestedTradeAmount,
            estimatedTransactionFeeAmount);

        return new RecommendationSnapshot
        {
            Id = Guid.NewGuid(),
            ModelRunId = modelRunId,
            PortfolioId = portfolioId,
            SecurityId = securityId,
            DataAsOfDate = dataAsOfDate,
            ClosePrice = closePrice,
            ModelDividendPerShare = modelDividendPerShare,
            DividendModeCode = normalizedDividendMode,
            ModelStatusCode = normalizedModelStatus,
            DividendReliabilityCode = normalizedReliability,
            ObservedPriceZoneCode = normalizedObservedPriceZone,
            PriceZoneCode = normalizedPriceZone,
            PriceZoneConfirmed = priceZoneConfirmed,
            RecommendationCode = normalizedRecommendation,
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

    private static void ValidateSnapshotSemantics(
        string? dividendModeCode,
        decimal? modelDividendPerShare,
        string modelStatusCode,
        string reliabilityCode,
        string? observedPriceZoneCode,
        string? priceZoneCode,
        bool priceZoneConfirmed,
        string recommendationCode,
        decimal? closePrice,
        int suggestedBuyShares,
        int suggestedSellShares,
        decimal suggestedTradeAmount,
        decimal estimatedTransactionFeeAmount)
    {
        if ((modelDividendPerShare is null) != (dividendModeCode is null))
        {
            throw new ArgumentException("模型股息和股息模式必须同时存在或同时为空。", nameof(dividendModeCode));
        }

        if (priceZoneConfirmed != (priceZoneCode is not null))
        {
            throw new ArgumentException(
                "价格区域确认标识必须与确认价格区域同时存在。",
                nameof(priceZoneConfirmed));
        }

        if (priceZoneConfirmed
            && !string.Equals(observedPriceZoneCode, priceZoneCode, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "确认价格区域必须与最新观测价格区域一致。",
                nameof(priceZoneCode));
        }

        if (modelStatusCode == ModelStatusCodes.Unavailable
            && (observedPriceZoneCode is not null || priceZoneCode is not null))
        {
            throw new ArgumentException(
                "模型不可用时不能保存价格区域。",
                nameof(priceZoneCode));
        }

        if (modelStatusCode == ModelStatusCodes.Available
            && reliabilityCode != DividendReliabilityCodes.Passed)
        {
            throw new ArgumentException(
                "模型可用时股息可靠性必须为 passed。",
                nameof(reliabilityCode));
        }

        var mustHaveNoTrade = modelStatusCode is
            ModelStatusCodes.Cautious
            or ModelStatusCodes.Failed
            or ModelStatusCodes.ReEvaluate
            or ModelStatusCodes.Unavailable;
        if (mustHaveNoTrade && (suggestedBuyShares > 0 || suggestedSellShares > 0))
        {
            throw new ArgumentException(
                "模型未处于可用状态时不能生成交易股数。",
                nameof(suggestedBuyShares));
        }

        var expectedRecommendation = modelStatusCode switch
        {
            ModelStatusCodes.Unavailable or ModelStatusCodes.Failed => RecommendationCodes.NoAction,
            ModelStatusCodes.ReEvaluate => RecommendationCodes.ReEvaluate,
            ModelStatusCodes.Cautious => recommendationCode is RecommendationCodes.Hold
                or RecommendationCodes.NoAction
                ? recommendationCode
                : throw new ArgumentException(
                    "谨慎参考状态只能使用 hold 或 no_action。",
                    nameof(recommendationCode)),
            _ when priceZoneCode is null => recommendationCode is RecommendationCodes.Hold
                or RecommendationCodes.NoAction
                ? recommendationCode
                : throw new ArgumentException(
                    "尚未确认价格区域时只能使用 hold 或 no_action。",
                    nameof(recommendationCode)),
            _ => priceZoneCode
        };
        if (!string.Equals(expectedRecommendation, recommendationCode, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "建议代码与模型状态或价格区域不一致。",
                nameof(recommendationCode));
        }

        if (suggestedBuyShares > 0 && suggestedSellShares > 0)
        {
            throw new ArgumentException("同一建议快照不能同时包含买入和卖出股数。", nameof(suggestedBuyShares));
        }

        if (suggestedBuyShares > 0 || suggestedSellShares > 0)
        {
            if (closePrice is null)
            {
                throw new ArgumentException("存在建议股数时必须提供收盘价。", nameof(closePrice));
            }

            var expectedTradeAmount =
                (suggestedBuyShares + suggestedSellShares) * closePrice.Value;
            if (suggestedTradeAmount != expectedTradeAmount)
            {
                throw new ArgumentException(
                    "建议交易金额必须等于建议股数乘以收盘价。",
                    nameof(suggestedTradeAmount));
            }
        }
        else if (suggestedTradeAmount != 0m || estimatedTransactionFeeAmount != 0m)
        {
            throw new ArgumentException(
                "没有建议股数时建议交易金额和手续费必须为零。",
                nameof(suggestedTradeAmount));
        }

        if (estimatedTransactionFeeAmount > 0m && suggestedTradeAmount == 0m)
        {
            throw new ArgumentException(
                "存在交易手续费时必须同时存在建议交易金额。",
                nameof(estimatedTransactionFeeAmount));
        }
    }
}
