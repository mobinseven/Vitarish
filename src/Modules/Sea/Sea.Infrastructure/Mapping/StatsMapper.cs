namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class StatsMapper
{
    public partial StatsDto ToDto(Stats src);
}

