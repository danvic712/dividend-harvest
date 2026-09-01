namespace DividendHarvest.Application.Exceptions;

public sealed class SetupValidationException(string message) : Exception(message);
