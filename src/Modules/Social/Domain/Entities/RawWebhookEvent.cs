namespace Vitarish.Social.Domain.Entities;

public class RawWebhookEvent : Entity
{
    public Guid? ConnectionId
    {
        get;
        set => SetField(ref field, value, nameof(ConnectionId));
    }

    public SocialProvider Provider
    {
        get;
        set => SetField(ref field, value, nameof(Provider));
    }

    public string IdempotencyKey
    {
        get;
        set => SetField(ref field, value, nameof(IdempotencyKey));
    } = string.Empty;

    public DateTimeOffset ReceivedAt
    {
        get;
        set => SetField(ref field, value, nameof(ReceivedAt));
    } = DateTimeOffset.UtcNow;

    public string RawHeaders
    {
        get;
        set => SetField(ref field, value, nameof(RawHeaders));
    } = "{}";

    public string RawBody
    {
        get;
        set => SetField(ref field, value, nameof(RawBody));
    } = string.Empty;

    public WebhookVerificationStatus VerificationStatus
    {
        get;
        set => SetField(ref field, value, nameof(VerificationStatus));
    } = WebhookVerificationStatus.Pending;

    public WebhookProcessingStatus ProcessingStatus
    {
        get;
        set => SetField(ref field, value, nameof(ProcessingStatus));
    } = WebhookProcessingStatus.Pending;

    public string? ProcessingError
    {
        get;
        set => SetField(ref field, value, nameof(ProcessingError));
    }

    public DateTimeOffset? ProcessedAt
    {
        get;
        set => SetField(ref field, value, nameof(ProcessedAt));
    }

    public int RetryCount
    {
        get;
        set => SetField(ref field, value, nameof(RetryCount));
    }
}
