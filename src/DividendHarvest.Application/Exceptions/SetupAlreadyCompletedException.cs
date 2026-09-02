namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("setup_already_completed")]
public sealed class SetupAlreadyCompletedException()
    : ApplicationExceptionBase("setup_already_completed");
