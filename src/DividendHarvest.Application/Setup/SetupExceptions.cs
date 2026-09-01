namespace DividendHarvest.Application.Setup;

public sealed class SetupValidationException(string message) : Exception(message);

public sealed class SetupAlreadyCompletedException()
    : InvalidOperationException("系统已经完成首次建账，不能重复初始化。");

public sealed class StockDataUnavailableException(string securityCode)
    : InvalidOperationException($"股票 {securityCode} 的基础资料暂时不可用。");
