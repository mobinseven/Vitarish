using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class RolesClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<RoleDto, RoleDto, RoleDescriptorDto, RolesClientDefinition>(connectionFactory, entityInterceptionProvider),
            IRolesClient
    {
        public async Task<Guid> CreateRoleAsync(string name, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<Guid>(nameof(CreateRoleAsync), cancellationToken, new object?[] { name }).ConfigureAwait(false);

        public async Task UpdateRoleNameAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UpdateRoleNameAsync), cancellationToken, id, name).ConfigureAwait(false);

        public async Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<(IReadOnlyList<RoleDto> Items, int TotalItems)>(nameof(GetPagedAsync), cancellationToken, new object?[] { descriptor }).ConfigureAwait(false);
    }
}
