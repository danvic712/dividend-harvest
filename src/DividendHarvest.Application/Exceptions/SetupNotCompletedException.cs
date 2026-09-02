namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("setup_not_completed")]
public sealed class SetupNotCompletedException()
    : ApplicationExceptionBase("setup_not_completed");
