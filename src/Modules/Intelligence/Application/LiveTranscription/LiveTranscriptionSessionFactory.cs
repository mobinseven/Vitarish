using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Application.LiveTranscription;

public sealed record LiveTranscriptionSessionCreationResult(
    string SessionId,
    LiveTranscriptionSession? Session,
    LiveTranscriptionError? Error)
{
    public bool Succeeded => Session is not null && Error is null;
}

public sealed class LiveTranscriptionSessionFactory(
    ITranscriptionProvider provider,
    LiveTranscriptionSettings settings,
    TimeProvider timeProvider)
{
    public LiveTranscriptionSessionCreationResult Create(
        LiveTranscriptionRequest request,
        Func<LiveTranscriptionSegment, CancellationToken, ValueTask> publishSegment,
        CancellationToken cancellationToken)
    {
        string sessionId = Guid.NewGuid().ToString("N");

        if (!settings.Enabled || !provider.IsAvailable)
        {
            return Failed(sessionId, LiveTranscriptionErrorCode.ProviderUnavailable,
                "Live transcription is unavailable.");
        }

        string? language = settings.Languages.FirstOrDefault(value =>
            string.Equals(value, request.Language?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (language is null)
        {
            return Failed(sessionId, LiveTranscriptionErrorCode.UnsupportedLanguage,
                "The requested transcription language is not supported.");
        }

        string? mimeType = settings.MimeTypes.FirstOrDefault(value =>
            string.Equals(value, request.MimeType?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (mimeType is null)
        {
            return Failed(sessionId, LiveTranscriptionErrorCode.UnsupportedMediaType,
                "The requested audio MIME type is not supported.");
        }

        LiveTranscriptionSession session = new(
            sessionId,
            provider,
            settings,
            timeProvider,
            new LiveTranscriptionProviderRequest(language, mimeType),
            publishSegment,
            cancellationToken);
        return new(sessionId, session, null);
    }

    private static LiveTranscriptionSessionCreationResult Failed(
        string sessionId,
        LiveTranscriptionErrorCode code,
        string message) => new(sessionId, null, new LiveTranscriptionError(code, message));
}
