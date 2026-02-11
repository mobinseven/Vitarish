namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class UserRolesMapper
{
    public partial UserRoleReadDto ToDto(UserRole src);
    public partial UserRole ToDomain(UserRoleWriteDto src);
    public partial UserRoleDescriptor ToDomain(UserRoleDescriptorDto src);
}

