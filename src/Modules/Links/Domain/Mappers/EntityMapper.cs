using Riok.Mapperly.Abstractions;

[assembly: Vitarish.Entities.Generators.GenerateEntitiesRegistrationAttribute]

namespace Vitarish.Links.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class EntityMapper
{
    public partial Link ToEntity(LinkWriteDto src);
    public partial LinkReadDto ToRead(Link src);
    public partial LinkWriteDto ToWrite(LinkReadDto src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial LinkDescriptor ToDescriptor(LinkDescriptorDto src);
}
