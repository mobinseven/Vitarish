using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class ProceduresClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<ProcedureDto, ProcedureDescriptorDto, ProceduresClientDefinition>(connectionFactory, entityInterceptionProvider),
            IProceduresClient
    {
        public async Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<Guid>(nameof(AddAsync), cancellationToken, new object?[] { projectId, name }).ConfigureAwait(false);

        public async Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UpdateAsync), cancellationToken, id, name).ConfigureAwait(false);

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(DeleteAsync), cancellationToken, id).ConfigureAwait(false);

        public async Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UpdateMetadataAsync), cancellationToken, id, metadata).ConfigureAwait(false);

        public async Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<ProcedureSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
