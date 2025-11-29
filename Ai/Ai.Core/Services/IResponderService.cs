namespace Vitarish.Ai.Core.Services;

using Vitarish.Ai.Core.Domain.Models;

/// <summary>
/// Service interface for AI responder operations.
/// </summary>
public interface IResponderService
{
    /// <summary>
    /// Gets a response from the AI asynchronously.
    /// </summary>
    /// <param name="inputs">The input data.</param>
    /// <param name="options">The responder options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of responder outputs.</returns>
    IAsyncEnumerable<ResponderOutput> GetResponseAsync(
        IEnumerable<ResponderInput> inputs,
        ResponderOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes audio to text.
    /// </summary>
    /// <param name="audioData">The audio data.</param>
    /// <param name="language">Optional language hint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcribed text.</returns>
    Task<string> TranscribeAudioAsync(
        byte[] audioData,
        string? language = null,
        CancellationToken cancellationToken = default);
}
