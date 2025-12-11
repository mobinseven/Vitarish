using System.Text.Json.Serialization;

namespace Vitarish.Clients.Modules.Intelligence.Models;

public sealed record AvailableModels
{
    [JsonPropertyName("transcriber_models")]
    public IReadOnlyList<ModelInfo> TranscriberModels { get; init; } = [];

    [JsonPropertyName("responder_models")]
    public IReadOnlyList<ModelInfo> ResponderModels { get; init; } = [];
}
