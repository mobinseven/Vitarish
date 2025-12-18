using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class EventRolesClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<EventRoleDto, EventRoleDto, EventRoleDescriptorDto, EventRolesClientDefinition>(connectionFactory, entityInterceptionProvider),
            IEventRolesClient
    {
        public async Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>(nameof(GetIdsAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>> GetIdsForEventsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)>>(nameof(GetIdsForEventsAsync), cancellationToken, new object?[] { eventIds }).ConfigureAwait(false);

        public async Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyList<Guid>>(nameof(GetReadIdsAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyList<Guid>>(nameof(GetWriteIdsAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(AssignAsync), cancellationToken, eventId, roleId, isWrite).ConfigureAwait(false);

        public async Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UnassignAsync), cancellationToken, eventId, roleId, isWrite).ConfigureAwait(false);
    }
}
