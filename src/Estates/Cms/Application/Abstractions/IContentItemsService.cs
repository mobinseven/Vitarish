namespace Vitarish.Cms.Application.Abstractions;

public interface IContentItemsService : IEntityService<ContentDto, ContentWriteDto, ContentDescriptorDto>
{
    Task PublishAsync(Guid id, DateTimeOffset publishedAt, CancellationToken cancellationToken);
    Task UnpublishAsync(Guid id, CancellationToken cancellationToken);
}
