using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class UserRolesHub(IUserRolesService service, IEntityUserService? entityUserService = null)
    : EntityServer<UserRoleDto, UserRoleWriteDto, UserRoleDescriptorDto, IUserRolesService>(service, entityUserService)
{
    public Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken) =>
        EntityService.AssignAsync(userId, roleId, type, cancellationToken);

    public Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken) =>
        EntityService.RemoveAsync(userId, roleId, cancellationToken);
}
