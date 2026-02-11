namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class EventsMapper
{
    public partial EventReadDto ToDto(Event src);
    public partial Event ToDomain(EventWriteDto src);
    public partial EventDescriptor ToDomain(EventDescriptorDto src);
}

