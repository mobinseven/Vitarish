using System.Threading.Channels;
using Vorn.Ai.Client.Contracts.Transcription;
using Vorn.Ai.Client.Models;

namespace Vorn.Ai.Client.Abstractions;

public interface ITranscriberClient
{
    Task<AvailableModels> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType = "audio/webm",
        string? language = null,
        TranscriberOptions? options = null,
        CancellationToken ct = default);
}
