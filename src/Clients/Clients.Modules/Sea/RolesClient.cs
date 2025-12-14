using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Internal
{
    internal sealed class RolesClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<RolesClientDefinition>(connectionFactory, entityInterceptionProvider),
            IRolesClient
    {
        public async Task<Guid> AddAsync(string name, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<Guid>(nameof(AddAsync), cancellationToken, new object?[] { name }).ConfigureAwait(false);

        public async Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UpdateAsync), cancellationToken, id, name).ConfigureAwait(false);

        public async Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<(IReadOnlyList<RoleDto> Items, int TotalItems)>(nameof(GetPagedAsync), cancellationToken, new object?[] { descriptor }).ConfigureAwait(false);
    }
}
