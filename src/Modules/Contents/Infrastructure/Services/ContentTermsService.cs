namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentTermsService(IMediator mediator)
    : EntityService<ContentTerm, ContentTermDescriptor, ContentTermReadDto, ContentTermWriteDto, ContentTermDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToRead,
        ContentsEntityMapperExtensions.ToEntity,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentTermsService
{
}
