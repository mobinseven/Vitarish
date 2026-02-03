namespace Vitarish.Cms.Infrastructure.Services;

[RegisterTransient]
internal sealed class TagsService(IMediator mediator, TagsMapper mapper)
    : EntityService<Tag, TagDescriptor, TagDto, TagWriteDto, TagDescriptorDto>(
        mediator,
        mapper.ToDto,
        mapper.ToDomain,
        mapper.ToDomain),
      ITagsService
{
}
