using System.Text.Json.Serialization;

namespace Vorn.Ai.Client.Models;

public sealed record AvailableModels
{
    [JsonPropertyName("transcriber_models")]
    public IReadOnlyList<ModelInfo> TranscriberModels { get; init; } = [];

    [JsonPropertyName("responder_models")]
    public IReadOnlyList<ModelInfo> ResponderModels { get; init; } = [];
}
