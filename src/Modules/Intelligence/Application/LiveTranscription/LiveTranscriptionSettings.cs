namespace Vitarish.Intelligence.Application.LiveTranscription;

public sealed class LiveTranscriptionSettings
{
    public bool Enabled { get; set; } = true;

    public string[] Languages { get; set; } = ["fa-IR", "en-US"];

    public string[] MimeTypes { get; set; } = ["audio/webm;codecs=opus", "audio/webm"];

    public int MaxFrameBytes { get; set; } = 64 * 1024;

    public long MaxTotalBytes { get; set; } = 12 * 1024 * 1024;

    public int MaxDurationSeconds { get; set; } = 270;

    public int IdleTimeoutSeconds { get; set; } = 20;

    public int ProviderOperationTimeoutSeconds { get; set; } = 285;

    public int ChannelCapacity { get; set; } = 8;

    public void Validate()
    {
        if (Languages is null || Languages.Length == 0 || Languages.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Live transcription requires at least one non-empty language.");
        }

        if (Languages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != Languages.Length)
        {
            throw new InvalidOperationException("Live transcription languages must be unique.");
        }

        if (MimeTypes is null || MimeTypes.Length == 0 || MimeTypes.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Live transcription requires at least one non-empty MIME type.");
        }

        if (MimeTypes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != MimeTypes.Length)
        {
            throw new InvalidOperationException("Live transcription MIME types must be unique.");
        }

        if (MaxFrameBytes is < 1024 or > 1024 * 1024)
        {
            throw new InvalidOperationException("Live transcription MaxFrameBytes must be between 1 KiB and 1 MiB.");
        }

        if (MaxTotalBytes < MaxFrameBytes || MaxTotalBytes > 100L * 1024 * 1024)
        {
            throw new InvalidOperationException("Live transcription MaxTotalBytes must contain at least one frame and be at most 100 MiB.");
        }

        if (MaxDurationSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException("Live transcription MaxDurationSeconds must be between 1 and 300.");
        }

        if (IdleTimeoutSeconds is < 1 || IdleTimeoutSeconds >= MaxDurationSeconds)
        {
            throw new InvalidOperationException("Live transcription IdleTimeoutSeconds must be positive and shorter than MaxDurationSeconds.");
        }

        if (ProviderOperationTimeoutSeconds < MaxDurationSeconds || ProviderOperationTimeoutSeconds > 360)
        {
            throw new InvalidOperationException("Live transcription provider timeout must cover the session duration and be at most 360 seconds.");
        }

        if (ChannelCapacity is < 1 or > 128)
        {
            throw new InvalidOperationException("Live transcription ChannelCapacity must be between 1 and 128.");
        }
    }
}
