namespace Vitarish.Cms.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentItemsService(
    IMediator mediator,
    ContentItemsMapper mapper)
    : EntityService<ContentItem, ContentDescriptor, ContentDto, ContentWriteDto, ContentDescriptorDto>(
        mediator,
        mapper.ToDto,
        mapper.ToDomain,
        mapper.ToDomain),
      IContentItemsService
{
    public async Task PublishAsync(Guid id, DateTimeOffset publishedAt, CancellationToken cancellationToken)
    {
        ContentItem? content = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (content is null)
        {
            return;
        }

        content.Publish(publishedAt);

        _ = (await UpdateEntityAsync(content, cancellationToken)).EnsureValue("Publish content");
    }

    public async Task UnpublishAsync(Guid id, CancellationToken cancellationToken)
    {
        ContentItem? content = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (content is null)
        {
            return;
        }

        content.Unpublish();

        _ = (await UpdateEntityAsync(content, cancellationToken)).EnsureValue("Unpublish content");
    }
}
