namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class DatabaseStatisticsMapper
{
    // Domain -> DTO
    public partial DatabaseStatisticsDto ToDto(DatabaseStatistics src);
}

