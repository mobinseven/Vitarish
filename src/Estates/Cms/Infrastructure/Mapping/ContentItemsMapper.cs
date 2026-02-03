namespace Vitarish.Cms.Infrastructure.Mapping;

[RegisterTransient]
internal sealed class ContentItemsMapper
{
    public ContentDto ToDto(ContentItem src)
        => new(
            src.Title,
            src.Slug,
            src.BodyHtml,
            src.Summary,
            src.AuthorId,
            src.Status,
            src.PublishedAt)
        {
            Id = src.Id,
            CreatedAt = src.CreatedAt,
            CreatedBy = src.CreatedBy,
            UpdatedAt = src.UpdatedAt,
            UpdatedBy = src.UpdatedBy,
            DeletedAt = src.DeletedAt,
            DeletedBy = src.DeletedBy
        };

    public ContentItem ToDomain(ContentWriteDto src)
    {
        ContentItem entity = new(src.Title, src.Slug, src.BodyHtml, src.Summary, src.AuthorId)
        {
            Id = src.Id
        };

        ApplyStatus(entity, src.Status, src.PublishedAt);

        return entity;
    }

    public ContentDescriptor ToDomain(ContentDescriptorDto src) => new()
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
        DeletedBy = src.DeletedBy,
        Status = src.Status,
        TagId = src.TagId,
        CategoryId = src.CategoryId
    };

    private static void ApplyStatus(ContentItem entity, ContentStatus status, DateTimeOffset? publishedAt)
    {
        switch (status)
        {
            case ContentStatus.Published:
                entity.Publish(publishedAt ?? DateTimeOffset.UtcNow);
                break;
            case ContentStatus.Archived:
                entity.Archive();
                break;
            default:
                entity.Unpublish();
                break;
        }
    }
}
