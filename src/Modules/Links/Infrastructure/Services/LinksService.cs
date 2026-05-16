namespace Vitarish.Links.Infrastructure.Services;

[RegisterTransient]
internal sealed class LinksService(IMediator mediator)
    : EntityService<Link, LinkDescriptor, LinkReadDto, LinkWriteDto, LinkDescriptorDto>(
        mediator,
        LinksEntityMapperExtensions.ToDto,
        LinksEntityMapperExtensions.ToDomain,
        LinksEntityMapperExtensions.ToDescriptor),
      ILinksService;
