namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentRevisionsService(IMediator mediator)
    : EntityService<ContentRevision, ContentRevisionDescriptor, ContentRevisionReadDto, ContentRevisionWriteDto, ContentRevisionDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToRead,
        ContentsEntityMapperExtensions.ToEntity,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentRevisionsService
{
}
