using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Clients.Modules.Sea.Enums;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Internal
{
    internal sealed class UserRolesClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<UserRolesClientDefinition>(connectionFactory, entityInterceptionProvider),
            IUserRolesClient
    {
        public async Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(AssignAsync), cancellationToken, userId, roleId, type).ConfigureAwait(false);

        public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(RemoveAsync), cancellationToken, userId, roleId).ConfigureAwait(false);
    }
}
