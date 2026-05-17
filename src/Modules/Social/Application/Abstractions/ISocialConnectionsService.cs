namespace Vitarish.Social.Application.Abstractions;

public interface ISocialConnectionsService
{
    Task<IReadOnlyList<SocialConnection>> GetConnectionsAsync(CancellationToken cancellationToken);
    Task<RawWebhookEvent?> GetLastWebhookAsync(Guid connectionId, CancellationToken cancellationToken);
}
