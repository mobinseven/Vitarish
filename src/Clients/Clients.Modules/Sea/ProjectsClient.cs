using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IProjectsClient
    {
        Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken = default);
        Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken = default);
        Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default);
        Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class ProjectsClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<ProjectsClientDefinition>(connectionFactory),
            IProjectsClient
    {
        public async Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeAsync<Guid>(nameof(AddAsync), cancellationToken, new object?[] { name, description, template, metadata }).ConfigureAwait(false);

        public async Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UpdateAsync), cancellationToken, new object?[] { id, name, description, template, metadata }).ConfigureAwait(false);

        public async Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(UpdateMetadataAsync), cancellationToken, new object?[] { id, metadata }).ConfigureAwait(false);

        public async Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeAsync<ProjectSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
