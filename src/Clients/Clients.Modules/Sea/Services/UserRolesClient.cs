using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Clients.Modules.Sea.Enums;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class UserRolesClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<UserRoleDto, UserRoleDto, UserRoleDescriptorDto, UserRolesClientDefinition>(connectionFactory, entityInterceptionProvider),
            IUserRolesClient
    {
        public async Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(AssignAsync), cancellationToken, userId, roleId, type).ConfigureAwait(false);

        public async Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(RemoveUserRoleAsync), cancellationToken, userId, roleId).ConfigureAwait(false);
    }
}
