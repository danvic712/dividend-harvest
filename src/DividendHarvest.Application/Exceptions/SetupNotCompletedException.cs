namespace DividendHarvest.Application.Exceptions;

public sealed class SetupNotCompletedException()
    : ApplicationExceptionBase(
        "setup_not_completed",
        "请先完成首次建账，再配置股票模型参数。");
