using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IEventRolesClient
    {
        Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default);
        Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class EventRolesClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<EventRolesClientDefinition>(connectionFactory),
            IEventRolesClient
    {
        public async Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>(nameof(GetIdsAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<Guid>>(nameof(GetReadIdsAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<Guid>>(nameof(GetWriteIdsAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(AssignAsync), cancellationToken, new object?[] { eventId, roleId, isWrite }).ConfigureAwait(false);

        public async Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UnassignAsync), cancellationToken, new object?[] { eventId, roleId, isWrite }).ConfigureAwait(false);
    }
}
