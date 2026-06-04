namespace Vitarish.Social.Domain.Entities;

public class SocialConnection : Entity
{
    public SocialProvider Provider
    {
        get;
        set => SetField(ref field, value, nameof(Provider));
    }

    public string ExternalAccountId
    {
        get;
        set => SetField(ref field, value, nameof(ExternalAccountId));
    } = string.Empty;

    public string WebhookSecret
    {
        get;
        set => SetField(ref field, value, nameof(WebhookSecret));
    } = string.Empty;

    public DateTime? WebhookVerifiedAt
    {
        get;
        set => SetField(ref field, value, nameof(WebhookVerifiedAt));
    }

    public SocialConnectionStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = SocialConnectionStatus.Active;

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];
}
