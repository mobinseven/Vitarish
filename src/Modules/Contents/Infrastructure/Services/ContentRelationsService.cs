namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentRelationsService(IMediator mediator)
    : EntityService<ContentRelation, ContentRelationDescriptor, ContentRelationReadDto, ContentRelationWriteDto, ContentRelationDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToDto,
        ContentsEntityMapperExtensions.ToDomain,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentRelationsService
{
}
