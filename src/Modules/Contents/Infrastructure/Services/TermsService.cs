namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class TermsService(IMediator mediator)
    : EntityService<Term, TermDescriptor, TermReadDto, TermWriteDto, TermDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToDto,
        ContentsEntityMapperExtensions.ToDomain,
        ContentsEntityMapperExtensions.ToDescriptor),
      ITermsService
{
}
