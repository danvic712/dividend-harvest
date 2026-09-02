namespace DividendHarvest.Application.Exceptions;

public sealed class ModelParameterVersionAlreadyExistsException(
    string securityCode,
    DateOnly effectiveFromDate)
    : InvalidOperationException(
        $"股票 {securityCode} 已存在生效日期为 {effectiveFromDate:yyyy-MM-dd} 的模型参数版本。");
