using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Clients.Modules.Sea.Enums;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IUserRolesClient : IEntityClient<UserRoleDto, UserRoleDescriptorDto>
{
    Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
