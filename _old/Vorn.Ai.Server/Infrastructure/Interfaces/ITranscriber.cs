using System.Threading.Channels;

namespace Vorn.Ai.Server.Infrastructure.Interfaces;

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
