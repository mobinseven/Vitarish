namespace Vorn.Ai.Client.Abstractions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

public interface ISpeechHubClient
{
    IAsyncEnumerable<string> StreamTranscribeAsync(ChannelReader<byte[]> audio, string mimeType = "audio/webm", string? language = null, CancellationToken ct = default);
}
