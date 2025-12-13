using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IProceduresClient
    {
        Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken = default);
        Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default);
        Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class ProceduresClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<ProceduresClientDefinition>(connectionFactory),
            IProceduresClient
    {
        public async Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken = default) =>
            await InvokeAsync<Guid>(nameof(AddAsync), cancellationToken, new object?[] { projectId, name }).ConfigureAwait(false);

        public async Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UpdateAsync), cancellationToken, new object?[] { id, name }).ConfigureAwait(false);

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(DeleteAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);

        public async Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UpdateMetadataAsync), cancellationToken, new object?[] { id, metadata }).ConfigureAwait(false);

        public async Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeAsync<ProcedureSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
