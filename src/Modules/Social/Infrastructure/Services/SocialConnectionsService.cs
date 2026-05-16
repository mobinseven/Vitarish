namespace Vitarish.Social.Infrastructure.Services;

[RegisterTransient]
internal sealed class SocialConnectionsService(
    IEntityRepository<SocialConnection, SocialConnectionDescriptor> connectionRepository,
    IEntityRepository<RawWebhookEvent, RawWebhookEventDescriptor> rawWebhookRepository) : ISocialConnectionsService
{
    public async Task<IReadOnlyList<SocialConnection>> GetConnectionsAsync(CancellationToken cancellationToken)
    {
        SocialConnectionDescriptor descriptor = new()
        {
            Skip = 0,
            Limit = 200,
        };

        return await connectionRepository.GetAsync(descriptor, cancellationToken);
    }

    public async Task<RawWebhookEvent?> GetLastWebhookAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        RawWebhookEventDescriptor descriptor = new()
        {
            ConnectionId = connectionId,
            Skip = 0,
            Limit = 1,
        };

        return (await rawWebhookRepository.GetAsync(descriptor, cancellationToken)).FirstOrDefault();
    }
}
