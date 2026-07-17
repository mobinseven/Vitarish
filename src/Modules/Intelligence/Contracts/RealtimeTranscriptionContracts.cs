using System.Text.Json.Serialization;

namespace Vitarish.Intelligence.Contracts;

public static class RealtimeTranscriptionProtocol
{
    public const string Subprotocol = "vitarish.live-transcription.v1";
}

public static class RealtimeTranscriptionMessageTypes
{
    public const string Start = "start";
    public const string Stop = "stop";
    public const string Started = "started";
    public const string Segment = "segment";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Canceled = "canceled";
}

[JsonConverter(typeof(JsonStringEnumConverter<RealtimeTranscriptionErrorCode>))]
public enum RealtimeTranscriptionErrorCode
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

public sealed record RealtimeTranscriptionLimits(
    int MaxFrameBytes,
    long MaxTotalBytes,
    int MaxDurationSeconds,
    int IdleTimeoutSeconds);

public sealed record RealtimeTranscriptionCapabilities(
    bool Available,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> MimeTypes,
    RealtimeTranscriptionLimits Limits);

public sealed record RealtimeTranscriptionClientMessage(
    string Type,
    string? Language = null,
    string? MimeType = null);

public sealed record RealtimeTranscriptionError(
    RealtimeTranscriptionErrorCode Code,
    string Message);

public sealed record RealtimeTranscriptionServerMessage(
    string Type,
    string SessionId,
    long? Sequence = null,
    string? Text = null,
    RealtimeTranscriptionError? Error = null);
