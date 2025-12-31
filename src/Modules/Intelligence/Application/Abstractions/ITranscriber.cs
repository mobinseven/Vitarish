using System.Threading.Channels;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface ITranscriber
{
    Task<string?> TranscribeAsync(Stream audio,
        string mimeType = "audio/webm",
        string? language = null,
        string? modelId = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType = "audio/webm",
        string? language = null,
        string? modelId = null,
        CancellationToken cancellationToken = default);
}
