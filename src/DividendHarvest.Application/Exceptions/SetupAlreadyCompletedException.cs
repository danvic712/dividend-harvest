namespace DividendHarvest.Application.Exceptions;

public sealed class SetupAlreadyCompletedException()
    : InvalidOperationException("系统已经完成首次建账，不能重复初始化。");
