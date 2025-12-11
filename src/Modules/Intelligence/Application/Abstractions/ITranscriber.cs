using System.Threading.Channels;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface ITranscriber
{
    Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, string? modelId, CancellationToken cancellationToken);

    IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType,
        string? language,
        string? modelId,
        CancellationToken cancellationToken);
}
