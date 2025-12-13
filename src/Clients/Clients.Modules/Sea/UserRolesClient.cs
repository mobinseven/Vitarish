using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IUserRolesClient
    {
        Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class UserRolesClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<UserRolesClientDefinition>(connectionFactory),
            IUserRolesClient
    {
        public async Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(AssignAsync), cancellationToken, new object?[] { userId, roleId, type }).ConfigureAwait(false);

        public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(RemoveAsync), cancellationToken, new object?[] { userId, roleId }).ConfigureAwait(false);
    }
}
