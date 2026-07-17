using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Application.LiveTranscription;

public sealed class LiveTranscriptionCapabilitiesQuery(
    ITranscriptionProvider provider,
    LiveTranscriptionSettings settings)
{
    public LiveTranscriptionCapabilities Execute() => new(
        settings.Enabled && provider.IsAvailable,
        settings.Languages,
        settings.MimeTypes,
        settings.MaxFrameBytes,
        settings.MaxTotalBytes,
        settings.MaxDurationSeconds,
        settings.IdleTimeoutSeconds);
}
