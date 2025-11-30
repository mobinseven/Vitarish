
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Vorn.Ai.Server.Contracts.Transcription;
using Vorn.Ai.Server.Domain.Models;
using Vorn.Ai.Server.Infrastructure.Interfaces;
using Vorn.Ai.Server.Infrastructure.Services.Dispatching;

namespace Vorn.Ai.Server.Presentation.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class SpeechHub(ITranscriber audioTranscriber, ModelRegistry modelRegistry) : Hub
{
    public Task<AvailableModels> GetAvailableModelsAsync()
    {
        return Task.FromResult(modelRegistry.GetAvailableModels());
    }

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
