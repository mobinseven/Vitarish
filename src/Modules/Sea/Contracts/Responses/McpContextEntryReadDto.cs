namespace Vitarish.Sea.Contracts.Responses;

public sealed class McpContextEntryReadDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = default!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = default!;
}

