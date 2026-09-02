namespace DividendHarvest.Application.Dtos;

public sealed record SetupStatus(
    bool IsComplete,
    IReadOnlyList<string> MissingRequirements);
