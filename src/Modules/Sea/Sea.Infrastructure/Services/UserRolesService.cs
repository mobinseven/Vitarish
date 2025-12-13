using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Infrastructure.Extensions;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterScoped]
internal class UserRolesService(IMediator mediator, UserRolesMapper mapper) : EntityService<UserRole, UserRoleDescriptor, UserRoleDto, UserRoleDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IUserRolesService
{
    public async Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken)
    {
        _ = (await AddAsync(
            new UserRole(userId, roleId, type),
            cancellationToken)).EnsureValue("Assign user role");
    }

    public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        (await RemoveByDescriptorAsync(
            new UserRoleDescriptor { UserId = userId, RoleId = roleId },
            cancellationToken)).EnsureSuccess("Remove user role");
    }
}
