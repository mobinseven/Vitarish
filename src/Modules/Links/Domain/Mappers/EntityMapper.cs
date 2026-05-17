using Riok.Mapperly.Abstractions;

[assembly: Vitarish.Entities.Generators.GenerateEntitiesRegistrationAttribute]

namespace Vitarish.Links.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class EntityMapper
{
    public partial Link ToDomain(LinkWriteDto src);
    public partial LinkReadDto ToDto(Link src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial LinkDescriptor ToDomain(LinkDescriptorDto src);
}
