using Vitarish.Social.Contracts.Responses;
using Vitarish.Social.Contracts.Results;

namespace Vitarish.Social.Contracts.Clients;

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

