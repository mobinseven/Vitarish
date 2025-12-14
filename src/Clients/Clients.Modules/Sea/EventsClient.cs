using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Internal
{
    internal sealed class EventsClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<EventsClientDefinition>(connectionFactory, entityInterceptionProvider),
            IEventsClient
    {
        public async Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyList<EventDto>>(nameof(GetByProcedureAsync), cancellationToken, new object?[] { procedureId }).ConfigureAwait(false);

        public async Task<EventDto?> AddAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<EventDto?>(nameof(AddAsync), cancellationToken, new object?[] { projectId, procedureId, previousEventId }).ConfigureAwait(false);

        public async Task<EventDto?> UpdateAsync(Guid id, string title, string description, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<EventDto?>(nameof(UpdateAsync), cancellationToken, new object?[] { id, title, description }).ConfigureAwait(false);

        public async Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<bool>(nameof(MoveUpAsync), cancellationToken, new object?[] { procedureId, eventId }).ConfigureAwait(false);

        public async Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<bool>(nameof(MoveDownAsync), cancellationToken, new object?[] { procedureId, eventId }).ConfigureAwait(false);

        public async Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<bool>(nameof(RemoveEventAsync), cancellationToken, new object?[] { procedureId, eventId }).ConfigureAwait(false);

        public async Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(RepairChainAsync), cancellationToken, procedureId).ConfigureAwait(false);

        public async Task UpdateChainAsync(Guid procedureId, IEnumerable<EventChainChangeDto> chainChangeDtos, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UpdateChainAsync), cancellationToken, procedureId, chainChangeDtos).ConfigureAwait(false);

        public async Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(CommitAsync), cancellationToken, eventId, reporterUserId, commitTime).ConfigureAwait(false);

        public async Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<int>(nameof(CommitAllByProcedureAsync), cancellationToken, new object?[] { procedureId, reporterUserId, commitTime }).ConfigureAwait(false);

        public async Task UncommitAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UncommitAsync), cancellationToken, eventId).ConfigureAwait(false);

        public async Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<int>(nameof(UncommitAllByProcedureAsync), cancellationToken, new object?[] { procedureId }).ConfigureAwait(false);

        public async Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<int>(nameof(GetRemindersCountAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<EventSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
