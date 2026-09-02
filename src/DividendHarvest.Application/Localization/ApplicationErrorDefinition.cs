using System.Text.Json.Serialization;

namespace DividendHarvest.Application.Localization;

internal sealed class ApplicationErrorDefinition
{
    [JsonPropertyName("status_code")]
    public int StatusCode { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;
}
