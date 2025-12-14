using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Internal
{
    internal sealed class ProjectsClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<ProjectsClientDefinition>(connectionFactory, entityInterceptionProvider),
            IProjectsClient
    {
        public async Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<Guid>(nameof(AddAsync), cancellationToken, new object?[] { name, description, template, metadata }).ConfigureAwait(false);

        public async Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UpdateAsync), cancellationToken, id, name, description, template, metadata).ConfigureAwait(false);

        public async Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(UpdateMetadataAsync), cancellationToken, id, metadata).ConfigureAwait(false);

        public async Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<ProjectSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
