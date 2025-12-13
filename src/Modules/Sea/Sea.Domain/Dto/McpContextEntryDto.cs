namespace Vitarish.Sea.Domain.Dto;

public sealed record McpContextEntryDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("value")] string Value);

