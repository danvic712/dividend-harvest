namespace DividendHarvest.Application.Exceptions;

public sealed class SetupAlreadyCompletedException()
    : ApplicationExceptionBase(
        "setup_already_completed",
        "系统已经完成首次建账，不能重复初始化。");
