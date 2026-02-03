namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class EventsMapper
{
    public partial EventDto ToDto(Event src);
    public partial Event ToDomain(EventWriteDto src);
    public partial EventDescriptor ToDomain(EventDescriptorDto src);
}

