namespace DividendHarvest.Application.Exceptions;

public sealed class ModelParameterValidationException(string message) : Exception(message);
