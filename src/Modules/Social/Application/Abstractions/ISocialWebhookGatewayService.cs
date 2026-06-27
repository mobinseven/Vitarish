using Vitarish.Social.Domain.Enums;

namespace Vitarish.Social.Application.Abstractions;

public interface ISocialWebhookGatewayService
{
    Task<Guid> IngestAsync(
        SocialProvider provider,
        Guid? connectionId,
        IReadOnlyDictionary<string, string> headers,
        string body,
        CancellationToken cancellationToken);

    Task<SocialWebhookReplayResult> ReplayAsync(Guid rawEventId, CancellationToken cancellationToken);
}
