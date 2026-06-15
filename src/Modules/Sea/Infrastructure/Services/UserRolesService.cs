using Vitarish.Sea.Domain.Mappers;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class UserRolesService(IMediator mediator) : EntityService<UserRole, UserRoleDescriptor, UserRoleReadDto, UserRoleWriteDto, UserRoleDescriptorDto>
    (
        mediator,
        userRole => userRole.ToRead(),
        userRole => userRole.ToEntity(),
        userRole => userRole.ToDescriptor()
    ),
    IUserRolesService
{
    public async Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken) => _ = (await AddAsync(
            new UserRole(userId, roleId, type),
            cancellationToken)).EnsureValue("Assign user role");

    public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken) => (await RemoveByDescriptorAsync(
            new UserRoleDescriptor { UserId = userId, RoleId = roleId },
            cancellationToken)).EnsureSuccess("Remove user role");
}
