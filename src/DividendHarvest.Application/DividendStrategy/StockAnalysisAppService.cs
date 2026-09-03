using DividendHarvest.Application.Contracts;
using DividendHarvest.Application.Dtos;
using DividendHarvest.Application.Exceptions;
using DividendHarvest.Application.Validators;
using DividendHarvest.Domain.Contracts;
using DividendHarvest.Domain.DividendModel;
using DividendHarvest.Domain.Codes;
using DividendHarvest.Domain.Models;
using DividendHarvest.Domain.Securities;
using FluentValidation;

namespace DividendHarvest.Application.DividendStrategy;

public sealed class StockAnalysisAppService(
    IUow uow,
    IValidator<GetStockAnalysisRequest> requestValidator,
    TimeProvider timeProvider) : IStockAnalysisAppService
{
    public async Task<StockAnalysisResult> GetAsync(
        GetStockAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validationResult = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw ApplicationErrors.Validation(
                ApplicationErrorCodes.StockAnalysisValidationFailed,
                ValidationErrorFormatter.Format(validationResult));
        }

        var reference = AShareReference.Create(request.SecurityCode, request.ExchangeCode);
        var security = await uow.Get<Security>()
            .SingleOrDefaultAsync(
                item =>
                    item.SecurityCode == reference.SecurityCode
                    && item.ExchangeCode == reference.ExchangeCode,
                cancellationToken);
        if (security is null)
        {
            throw ApplicationErrors.WithSecurityReference(
                ApplicationErrorCodes.StockNotConfigured,
                reference.SecurityCode,
                reference.ExchangeCode);
        }

        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var parameters = await uow.Get<ModelParameterSet>()
            .FirstOrDefaultAsync(
                parameter =>
                    parameter.SecurityId == security.Id
                    && parameter.EffectiveFromDate <= currentDate,
                orderBy: parameter => parameter.EffectiveFromDate,
                descending: true,
                cancellationToken: cancellationToken);
        var priceObservations = await uow.Get<PriceObservation>()
            .ListAsync(
                observation =>
                    observation.SecurityId == security.Id
                    && observation.TradingDate <= currentDate
                    && observation.DataQualityCode == DataQualityCodes.Valid,
                orderBy: [observation => observation.TradingDate],
                descending: true,
                cancellationToken: cancellationToken);
        var priceObservation = priceObservations.FirstOrDefault();
        var dividendEvents = await uow.Get<DividendEvent>()
            .ListAsync(
                dividendEvent => dividendEvent.SecurityId == security.Id,
                cancellationToken: cancellationToken);
        var financialSnapshots = await uow.Get<FinancialSnapshot>()
            .ListAsync(
                snapshot => snapshot.SecurityId == security.Id,
                cancellationToken: cancellationToken);
        var position = await uow.Get<PortfolioPosition>()
            .FirstOrDefaultAsync(
                currentPosition => currentPosition.SecurityId == security.Id
                    && (parameters == null
                        || currentPosition.PortfolioId == parameters.PortfolioId),
                cancellationToken: cancellationToken);

        var heldShares = position?.HeldShares ?? 0;
        var coreShares = position?.CoreShares ?? 0;
        var satelliteShares = Math.Max(heldShares - coreShares, 0);
        var modelDividendPerShare = priceObservation is null
            ? null
            : TtmDividendCalculator.Calculate(
                dividendEvents,
                priceObservation.TradingDate);
        var reliabilityCode = modelDividendPerShare is null
            ? DividendReliabilityCodes.Unavailable
            : DividendReliabilityEvaluator.Evaluate(
                dividendEvents,
                financialSnapshots,
                priceObservation!.TradingDate);
        var computedAt = timeProvider.GetUtcNow();

        if (parameters is null
            || priceObservation is null
            || modelDividendPerShare is null)
        {
            return CreateUnavailableResult(
                security,
                reference,
                priceObservation,
                modelDividendPerShare,
                reliabilityCode,
                heldShares,
                coreShares,
                satelliteShares,
                computedAt);
        }

        var hasRecentCancellation = DividendReliabilityEvaluator.HasRecentCancellation(
            dividendEvents,
            priceObservation.TradingDate);

        var modelStatusCode = hasRecentCancellation
            ? ModelStatusCodes.ReEvaluate
            : reliabilityCode switch
            {
                DividendReliabilityCodes.Passed => ModelStatusCodes.Available,
                DividendReliabilityCodes.Failed => ModelStatusCodes.Failed,
                _ => ModelStatusCodes.Cautious
            };
        var priceZoneValues = DividendPriceZoneCalculator.Calculate(
            parameters,
            modelDividendPerShare.Value,
            priceObservation.ClosePrice);
        var priceZoneConfirmation = PriceZoneConfirmationCalculator.Calculate(
            parameters,
            modelDividendPerShare.Value,
            priceObservations);
        var recommendationCode = GetRecommendationCode(
            modelStatusCode,
            priceZoneConfirmation.ConfirmedPriceZoneCode);
        var explanation = BuildExplanation(
            modelStatusCode,
            priceZoneConfirmation.IsConfirmed,
            priceZoneConfirmation.ConfirmedPriceZoneCode);

        return new StockAnalysisResult(
            reference.SecurityCode,
            reference.ExchangeCode,
            GetDisplaySecurityName(security, reference),
            modelStatusCode,
            reliabilityCode,
            priceObservation.ClosePrice,
            modelDividendPerShare,
            DividendModeCodes.Ttm,
            priceZoneValues.DividendYield,
            priceZoneValues.StrongBuyPrice,
            priceZoneValues.AccumulatePrice,
            priceZoneValues.PartialTrimPrice,
            priceZoneValues.AggressiveTrimPrice,
            priceZoneConfirmation.ObservedPriceZoneCode,
            priceZoneConfirmation.ConfirmedPriceZoneCode,
            priceZoneConfirmation.IsConfirmed,
            recommendationCode,
            heldShares,
            coreShares,
            satelliteShares,
            priceObservation.TradingDate,
            parameters.Id,
            computedAt,
            explanation,
            security.Id);
    }

    private static string GetRecommendationCode(
        string modelStatusCode,
        string? confirmedPriceZoneCode)
        => modelStatusCode switch
        {
            ModelStatusCodes.ReEvaluate => RecommendationCodes.ReEvaluate,
            ModelStatusCodes.Failed or ModelStatusCodes.Unavailable => RecommendationCodes.NoAction,
            ModelStatusCodes.Cautious => RecommendationCodes.Hold,
            _ => confirmedPriceZoneCode ?? RecommendationCodes.Hold
        };

    private static string BuildExplanation(
        string modelStatusCode,
        bool priceZoneConfirmed,
        string? confirmedPriceZoneCode)
        => modelStatusCode switch
        {
            ModelStatusCodes.Unavailable =>
                "缺少有效模型参数、行情或 TTM 实际股息，暂不生成价格区域和交易建议。",
            ModelStatusCodes.ReEvaluate =>
                "最近存在已确认取消分红的事件，需要重新评估核心仓和后续操作，当前不生成交易建议。",
            ModelStatusCodes.Failed =>
                "股息可靠性检查未通过，当前只展示行情和持仓信息，不生成交易建议。",
            ModelStatusCodes.Cautious =>
                "股息率和价格区域可以计算，但可靠性资料不足或存在风险提醒，当前仅谨慎持有。",
            _ when !priceZoneConfirmed =>
                "模型资料完整，但新的价格区域尚未连续两个有效交易日确认，当前仅观察。",
            _ => $"股息可靠性检查通过，已确认当前价格区域为 {confirmedPriceZoneCode}。"
        };

    private static StockAnalysisResult CreateUnavailableResult(
        Security security,
        AShareReference reference,
        PriceObservation? priceObservation,
        decimal? modelDividendPerShare,
        string reliabilityCode,
        int heldShares,
        int coreShares,
        int satelliteShares,
        DateTimeOffset computedAt)
        => new(
            reference.SecurityCode,
            reference.ExchangeCode,
            GetDisplaySecurityName(security, reference),
            ModelStatusCodes.Unavailable,
            reliabilityCode,
            priceObservation?.ClosePrice,
            modelDividendPerShare,
            modelDividendPerShare is null ? null : DividendModeCodes.Ttm,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            RecommendationCodes.NoAction,
            heldShares,
            coreShares,
            satelliteShares,
            priceObservation?.TradingDate,
            null,
            computedAt,
            "缺少有效模型参数、行情或 TTM 实际股息，暂不生成价格区域和交易建议。",
            security.Id);

    private static string GetDisplaySecurityName(
        Security security,
        AShareReference reference)
        => string.IsNullOrWhiteSpace(security.SecurityName)
            ? $"待同步 {reference.SecurityCode}"
            : security.SecurityName;
}
