namespace Vitarish.Sea.Domain.Dto;

public sealed record McpContextEntryReadDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("value")] string Value);

