namespace Vitarish.Sea.Domain.Dto;

public record ProjectWriteDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("template")] bool Template) : EntityWriteDto;
