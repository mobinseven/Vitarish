namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class UsageConfigurationMapper
{
    // Entity -> DTO
    public partial UsageConfigurationDto ToDto(UsageConfiguration src);
    public partial UsageConfiguration ToDomain(UsageConfigurationDto src);
}

