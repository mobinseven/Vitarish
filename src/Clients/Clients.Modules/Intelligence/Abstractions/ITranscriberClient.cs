using System.Threading.Channels;
using Vitarish.Clients.Modules.Intelligence.Contracts;
using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Clients.Modules.Intelligence.Abstractions;

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
