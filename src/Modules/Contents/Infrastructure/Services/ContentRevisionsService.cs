namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentRevisionsService(IMediator mediator)
    : EntityService<ContentRevision, ContentRevisionDescriptor, ContentRevisionReadDto, ContentRevisionWriteDto, ContentRevisionDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToDto,
        ContentsEntityMapperExtensions.ToDomain,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentRevisionsService
{
}
