namespace Vitarish.Cms.Domain.Dto;

public record CategoryWriteDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("parentCategoryId")] Guid? ParentCategoryId) : EntityWriteDto;
