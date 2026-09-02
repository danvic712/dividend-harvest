namespace DividendHarvest.Application.Exceptions;

[ApplicationErrorCode("budget_validation_failed")]
public sealed class BudgetValidationException(string message)
    : ApplicationValidationException(message);
