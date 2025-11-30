
using System.Text.Json.Serialization;

namespace Vorn.Ai.Server.Domain.Models;

public sealed record ModelInfo
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("display_name")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("provider")]
    public required string Provider { get; init; }
}
