namespace Vitarish.Cms.Domain.Dto;

public record CategoryDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("parentCategoryId")] Guid? ParentCategoryId) : EntityDto;
