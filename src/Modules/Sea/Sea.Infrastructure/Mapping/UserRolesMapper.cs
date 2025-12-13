namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class UserRolesMapper
{
    public partial UserRoleDto ToDto(UserRole src);
    public partial UserRole ToDomain(UserRoleDto src);
    public partial UserRoleDescriptor ToDomain(UserRoleDescriptorDto src);
}

