namespace Vitarish.Cms.Domain.Dto;

public record TagWriteDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("description")] string? Description) : EntityWriteDto;
