namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class RolesMapper
{
    public partial RoleDto ToDto(Role src);
    public partial Role ToDomain(RoleWriteDto src);
    public partial RoleDescriptor ToDomain(RoleDescriptorDto src);
}

