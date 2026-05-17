namespace Vitarish.Social.Application.Abstractions;

public interface ISocialProviderAdapter
{
    SocialProvider Provider { get; }
    IReadOnlySet<SocialCapability> Capabilities { get; }

    bool VerifyWebhook(IReadOnlyDictionary<string, string> headers, string body, string secret);
    SocialEvent? ParseWebhook(SocialConnection connection, RawWebhookEvent rawEvent);
    Task<SocialProfile?> FetchProfileAsync(SocialConnection connection, string accessToken, CancellationToken cancellationToken);
}
