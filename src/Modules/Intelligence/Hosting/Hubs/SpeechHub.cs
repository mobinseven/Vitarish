using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Vitarish.Intelligence.Hosting.Hubs;

public sealed class SpeechHub(ITranscriber audioTranscriber, ModelRegistry modelRegistry) : Hub
{
    public Task<AvailableModels> GetAvailableModelsAsync() => Task.FromResult(modelRegistry.GetAvailableModels());

    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType,
        string? language,
        TranscriberOptions? options,
       [EnumeratorCancellation] CancellationToken cancellationToken, CancellationToken ct)
    {
        await foreach (string piece in audioTranscriber.StreamTranscribeAsync(audio, mimeType, language, options?.ModelId, cancellationToken))
        {
            yield return piece;
        }
    }
}
