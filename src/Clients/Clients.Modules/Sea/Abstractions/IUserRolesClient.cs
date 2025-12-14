using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IUserRolesClient
{
    Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
