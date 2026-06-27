namespace Vitarish.Social.Contracts;

public sealed record SocialQueryResult<T>(T? Value, SocialErrorResponse? Error)
{
    public bool IsSuccess => Error is null;
    public static SocialQueryResult<T> Success(T value) => new(value, null);
    public static SocialQueryResult<T> Failed(SocialErrorResponse error) => new(default, error);
}

public sealed record SocialReplayWebhookResponse(SocialReplayWebhookStatus Status, SocialErrorResponse? Error = null)
{
    public bool IsSuccess => Status == SocialReplayWebhookStatus.Queued;
    public static SocialReplayWebhookResponse Queued() => new(SocialReplayWebhookStatus.Queued);
    public static SocialReplayWebhookResponse Failed(SocialReplayWebhookStatus status, SocialErrorResponse error) => new(status, error);
}

