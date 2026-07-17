namespace Vitarish.Intelligence.Application.LiveTranscription;

public enum TranscriptionProviderEventKind
{
    Ready,
    FinalSegment
}

public sealed record TranscriptionProviderEvent(
    TranscriptionProviderEventKind Kind,
    string? Text = null)
{
    public static TranscriptionProviderEvent Ready() => new(TranscriptionProviderEventKind.Ready);

    public static TranscriptionProviderEvent FinalSegment(string text) =>
        new(TranscriptionProviderEventKind.FinalSegment, text);
}

public sealed record LiveTranscriptionProviderRequest(string Language, string MimeType);

public sealed record LiveTranscriptionRequest(string Language, string MimeType);

public sealed record LiveTranscriptionSegment(long Sequence, string Text);

public enum LiveTranscriptionSessionState
{
    Connecting,
    Active,
    Completing,
    Completed,
    Failed,
    Canceled
}

public enum LiveTranscriptionErrorCode
{
    InvalidStart,
    ProtocolViolation,
    UnsupportedLanguage,
    UnsupportedMediaType,
    FrameTooLarge,
    PayloadTooLarge,
    DurationLimitExceeded,
    IdleTimeout,
    ProviderUnavailable,
    ProviderTimeout,
    InvalidAudio,
    Cancellation,
    UnexpectedFailure
}

public sealed record LiveTranscriptionError(LiveTranscriptionErrorCode Code, string Message);

public enum LiveTranscriptionOutcomeKind
{
    Completed,
    Failed,
    Canceled
}

public sealed record LiveTranscriptionOutcome(
    LiveTranscriptionOutcomeKind Kind,
    LiveTranscriptionError? Error = null);

public sealed record LiveTranscriptionAcceptResult(
    bool Accepted,
    LiveTranscriptionError? Error = null)
{
    public static LiveTranscriptionAcceptResult Success { get; } = new(true);

    public static LiveTranscriptionAcceptResult Rejected(LiveTranscriptionError error) =>
        new(false, error);
}

public sealed record LiveTranscriptionCapabilities(
    bool Available,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> MimeTypes,
    int MaxFrameBytes,
    long MaxTotalBytes,
    int MaxDurationSeconds,
    int IdleTimeoutSeconds);

public sealed class TranscriptionProviderException : Exception
{
    public TranscriptionProviderException(
        LiveTranscriptionErrorCode code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public LiveTranscriptionErrorCode Code { get; }
}
