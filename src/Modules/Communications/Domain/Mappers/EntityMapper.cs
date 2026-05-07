using Riok.Mapperly.Abstractions;

[assembly: Vitarish.Entities.Generators.GenerateEntitiesRegistrationAttribute]

namespace Vitarish.Communications.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class EntityMapper
{
}
