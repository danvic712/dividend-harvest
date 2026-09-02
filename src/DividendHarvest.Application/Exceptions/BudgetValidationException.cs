namespace DividendHarvest.Application.Exceptions;

public sealed class BudgetValidationException(string message)
    : InvalidOperationException(message);
