namespace DividendHarvest.Application.Localization;

public sealed record LocalizedApplicationError(
    string ErrorCode,
    string CultureName,
    int StatusCode,
    string Title,
    string Detail);
