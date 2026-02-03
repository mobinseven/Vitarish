namespace Vitarish.Cms.Infrastructure.Mapping;

[RegisterTransient]
internal sealed class TagsMapper
{
    public TagDto ToDto(Tag src)
        => new(src.Name, src.Slug, src.Description)
        {
            Id = src.Id,
            CreatedAt = src.CreatedAt,
            CreatedBy = src.CreatedBy,
            UpdatedAt = src.UpdatedAt,
            UpdatedBy = src.UpdatedBy,
            DeletedAt = src.DeletedAt,
            DeletedBy = src.DeletedBy
        };

    public Tag ToDomain(TagWriteDto src)
        => new(src.Name, src.Slug, src.Description)
        {
            Id = src.Id
        };

    public TagDescriptor ToDomain(TagDescriptorDto src) => new()
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
