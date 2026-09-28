using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Application.LiveTranscription;

namespace Vitarish.E2e.Infrastructure.Fixtures;

internal sealed class E2eLiveTranscriptionProvider : ITranscriptionProvider
{
    public bool IsAvailable => true;

    public async Task TranscribeAsync(
        ChannelReader<byte[]> audio,
        LiveTranscriptionProviderRequest request,
        Func<TranscriptionProviderEvent, CancellationToken, ValueTask> publish,
        CancellationToken cancellationToken)
    {
        _ = request;
        await publish(TranscriptionProviderEvent.Ready(), cancellationToken);
        bool receivedAudio = false;
        await foreach (byte[] frame in audio.ReadAllAsync(cancellationToken))
            receivedAudio |= frame.Length > 0;

        if (receivedAudio)
            await publish(TranscriptionProviderEvent.FinalSegment("E2E live transcription verified"), cancellationToken);
    }
}
