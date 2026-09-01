namespace DividendHarvest.Application.Dto;

public sealed record SetupStatus(
    bool IsComplete,
    IReadOnlyList<string> MissingRequirements);
