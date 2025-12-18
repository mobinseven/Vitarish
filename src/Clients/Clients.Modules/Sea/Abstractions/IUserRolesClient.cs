using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Clients.Modules.Sea.Enums;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IUserRolesClient : IEntityClient<UserRoleDto, UserRoleDto, UserRoleDescriptorDto>
{
    Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken = default);
    Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
