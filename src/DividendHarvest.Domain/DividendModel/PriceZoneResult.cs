namespace DividendHarvest.Domain.DividendModel;

public sealed record PriceZoneResult(
    decimal StrongBuyPrice,
    decimal AccumulatePrice,
    decimal PartialTrimPrice,
    decimal AggressiveTrimPrice,
    decimal DividendYield,
    string PriceZoneCode);
