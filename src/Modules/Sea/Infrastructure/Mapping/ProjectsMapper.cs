namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class ProjectsMapper
{
    public partial ProjectDto ToDto(Project src);

    public partial Project ToDomain(ProjectDto src);

    public partial ProjectDescriptor ToDomain(ProjectDescriptorDto src);
}

