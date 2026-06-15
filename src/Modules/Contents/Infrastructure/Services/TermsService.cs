namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class TermsService(IMediator mediator)
    : EntityService<Term, TermDescriptor, TermReadDto, TermWriteDto, TermDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToRead,
        ContentsEntityMapperExtensions.ToEntity,
        ContentsEntityMapperExtensions.ToDescriptor),
      ITermsService
{
}
