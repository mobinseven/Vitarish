using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Clients.Modules.Intelligence.Abstractions;
/// <summary>
/// Provides access to available AI models for transcription and response generation.
/// </summary>
public interface IModelRegistry
{
    /// <summary>
    /// Gets the cached available models, or null if not yet loaded.
    /// </summary>
    AvailableModels? CachedModels { get; }

    /// <summary>
    /// Gets a value indicating whether models have been loaded from the server.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Loads available models from the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The available models.</returns>
    Task<AvailableModels> LoadModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the available models from the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refreshed available models.</returns>
    Task<AvailableModels> RefreshModelsAsync(CancellationToken cancellationToken = default);
}
