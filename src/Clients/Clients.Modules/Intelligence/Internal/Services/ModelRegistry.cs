using Vitarish.Clients.Modules.Intelligence.Abstractions;
using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Clients.Modules.Intelligence.Internal.Services;

internal sealed class ModelRegistry(IResponderClient responseHubClient) : IModelRegistry
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AvailableModels? CachedModels { get; private set; }

    public bool IsLoaded => CachedModels is not null;

    public async Task<AvailableModels> LoadModelsAsync(CancellationToken cancellationToken = default) => CachedModels is not null ? CachedModels : await RefreshModelsAsync(cancellationToken).ConfigureAwait(false);

    public async Task<AvailableModels> RefreshModelsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CachedModels = await responseHubClient.GetAvailableModelsAsync(cancellationToken).ConfigureAwait(false);
            return CachedModels;
        }
        finally
        {
            _ = _gate.Release();
        }
    }
}
