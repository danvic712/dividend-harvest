namespace DividendHarvest.Application.Exceptions;

public sealed class BudgetValidationException(string message)
    : ApplicationValidationException("budget_validation_failed", message);
