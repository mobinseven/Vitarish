namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class EventRolesMapper
{
    public partial EventRoleReadDto ToDto(EventRole src);
    public partial EventRole ToDomain(EventRoleWriteDto src);
    public partial EventRoleDescriptor ToDomain(EventRoleDescriptorDto src);
}
