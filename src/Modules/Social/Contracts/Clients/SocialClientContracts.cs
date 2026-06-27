namespace Vitarish.Social.Contracts;

public interface ISocialClient : ISocialConnectionsClient, ISocialWebhooksClient;

public interface ISocialConnectionsClient
{
    Task<SocialQueryResult<IReadOnlyList<SocialConnectionResponse>>> GetConnectionsAsync(CancellationToken cancellationToken);
    Task<SocialQueryResult<SocialWebhookEventResponse?>> GetLastWebhookAsync(Guid connectionId, CancellationToken cancellationToken);
}

public interface ISocialWebhooksClient
{
    Task<SocialReplayWebhookResponse> ReplayAsync(Guid rawWebhookEventId, CancellationToken cancellationToken);
}

