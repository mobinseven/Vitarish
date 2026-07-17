using System.Threading.Channels;
using Vitarish.Intelligence.Application.LiveTranscription;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface ITranscriptionProvider
{
    bool IsAvailable { get; }

    Task TranscribeAsync(
        ChannelReader<byte[]> audio,
        LiveTranscriptionProviderRequest request,
        Func<TranscriptionProviderEvent, CancellationToken, ValueTask> publish,
        CancellationToken cancellationToken);
}
