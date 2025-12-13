using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IRolesClient
    {
        Task<Guid> AddAsync(string name, CancellationToken cancellationToken = default);
        Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class RolesClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<RolesClientDefinition>(connectionFactory),
            IRolesClient
    {
        public async Task<Guid> AddAsync(string name, CancellationToken cancellationToken = default) =>
            await InvokeAsync<Guid>(nameof(AddAsync), cancellationToken, new object?[] { name }).ConfigureAwait(false);

        public async Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UpdateAsync), cancellationToken, new object?[] { id, name }).ConfigureAwait(false);

        public async Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken = default) =>
            await InvokeAsync<(IReadOnlyList<RoleDto> Items, int TotalItems)>(nameof(GetPagedAsync), cancellationToken, new object?[] { descriptor }).ConfigureAwait(false);
    }
}
