using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IEventsClient
    {
        Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default);
        Task<EventDto?> AddAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken = default);
        Task<EventDto?> UpdateAsync(Guid id, string title, string description, CancellationToken cancellationToken = default);
        Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default);
        Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default);
        Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default);
        Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken = default);
        Task UpdateChainAsync(Guid procedureId, IEnumerable<EventChainChangeDto> chainChangeDtos, CancellationToken cancellationToken = default);
        Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default);
        Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default);
        Task UncommitAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default);
        Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class EventsClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<EventsClientDefinition>(connectionFactory),
            IEventsClient
    {
        public async Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<EventDto>>(nameof(GetByProcedureAsync), cancellationToken, new object?[] { procedureId }).ConfigureAwait(false);

        public async Task<EventDto?> AddAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<EventDto?>(nameof(AddAsync), cancellationToken, new object?[] { projectId, procedureId, previousEventId }).ConfigureAwait(false);

        public async Task<EventDto?> UpdateAsync(Guid id, string title, string description, CancellationToken cancellationToken = default) =>
            await InvokeAsync<EventDto?>(nameof(UpdateAsync), cancellationToken, new object?[] { id, title, description }).ConfigureAwait(false);

        public async Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<bool>(nameof(MoveUpAsync), cancellationToken, new object?[] { procedureId, eventId }).ConfigureAwait(false);

        public async Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<bool>(nameof(MoveDownAsync), cancellationToken, new object?[] { procedureId, eventId }).ConfigureAwait(false);

        public async Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<bool>(nameof(RemoveEventAsync), cancellationToken, new object?[] { procedureId, eventId }).ConfigureAwait(false);

        public async Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(RepairChainAsync), cancellationToken, new object?[] { procedureId }).ConfigureAwait(false);

        public async Task UpdateChainAsync(Guid procedureId, IEnumerable<EventChainChangeDto> chainChangeDtos, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UpdateChainAsync), cancellationToken, new object?[] { procedureId, chainChangeDtos }).ConfigureAwait(false);

        public async Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(CommitAsync), cancellationToken, new object?[] { eventId, reporterUserId, commitTime }).ConfigureAwait(false);

        public async Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken = default) =>
            await InvokeAsync<int>(nameof(CommitAllByProcedureAsync), cancellationToken, new object?[] { procedureId, reporterUserId, commitTime }).ConfigureAwait(false);

        public async Task UncommitAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UncommitAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<int>(nameof(UncommitAllByProcedureAsync), cancellationToken, new object?[] { procedureId }).ConfigureAwait(false);

        public async Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<int>(nameof(GetRemindersCountAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeAsync<EventSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
