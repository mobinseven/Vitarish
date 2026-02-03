namespace Vitarish.Cms.Infrastructure.Mapping;

[RegisterTransient]
internal sealed class CategoriesMapper
{
    public CategoryDto ToDto(Category src)
        => new(src.Name, src.Slug, src.ParentCategoryId)
        {
            Id = src.Id,
            CreatedAt = src.CreatedAt,
            CreatedBy = src.CreatedBy,
            UpdatedAt = src.UpdatedAt,
            UpdatedBy = src.UpdatedBy,
            DeletedAt = src.DeletedAt,
            DeletedBy = src.DeletedBy
        };

    public Category ToDomain(CategoryWriteDto src)
        => new(src.Name, src.Slug, src.ParentCategoryId)
        {
            Id = src.Id
        };

    public CategoryDescriptor ToDomain(CategoryDescriptorDto src) => new()
    {
        Id = src.Id,
        Skip = src.Skip,
        Take = src.Take,
        Search = src.Search,
        CreatedFrom = src.CreatedFrom,
        CreatedTo = src.CreatedTo,
        CreatedBy = src.CreatedBy,
        UpdatedFrom = src.UpdatedFrom,
        UpdatedTo = src.UpdatedTo,
        UpdatedBy = src.UpdatedBy,
        DeletedFrom = src.DeletedFrom,
        DeletedTo = src.DeletedTo,
        DeletedBy = src.DeletedBy
    };
}
