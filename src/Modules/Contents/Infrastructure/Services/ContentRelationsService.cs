namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentRelationsService(IMediator mediator)
    : EntityService<ContentRelation, ContentRelationDescriptor, ContentRelationReadDto, ContentRelationWriteDto, ContentRelationDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToRead,
        ContentsEntityMapperExtensions.ToEntity,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentRelationsService
{
}
