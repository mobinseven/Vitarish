namespace Vitarish.Social.Application.Abstractions;

public enum SocialWebhookReplayStatus
{
    Queued,
    NotFound,
    InvalidState,
}

public sealed record SocialWebhookReplayResult(SocialWebhookReplayStatus Status)
{
    public bool IsQueued => Status == SocialWebhookReplayStatus.Queued;
    public static SocialWebhookReplayResult Queued() => new(SocialWebhookReplayStatus.Queued);
    public static SocialWebhookReplayResult NotFound() => new(SocialWebhookReplayStatus.NotFound);
    public static SocialWebhookReplayResult InvalidState() => new(SocialWebhookReplayStatus.InvalidState);
}
