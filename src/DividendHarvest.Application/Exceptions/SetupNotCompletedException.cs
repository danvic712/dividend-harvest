namespace DividendHarvest.Application.Exceptions;

public sealed class SetupNotCompletedException()
    : InvalidOperationException("请先完成首次建账，再配置股票模型参数。");
