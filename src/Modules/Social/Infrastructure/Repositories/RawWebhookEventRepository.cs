using Vitarish.Entities.Server.Data;
namespace Vitarish.Social.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class RawWebhookEventRepository(SocialDbContext dbContext)
    : EntityRepository<RawWebhookEvent, RawWebhookEventDescriptor, SocialDbContext>(dbContext),
      IEntityRepository<RawWebhookEvent, RawWebhookEventDescriptor>
{
    protected override IQueryable<RawWebhookEvent> Apply(IQueryable<RawWebhookEvent> query, RawWebhookEventDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Provider is { } provider)
        {
            query = query.Where(e => e.Provider == provider);
        }

        if (descriptor.ConnectionId is { } connectionId)
        {
            query = query.Where(e => e.ConnectionId == connectionId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.IdempotencyKey))
        {
            string key = descriptor.IdempotencyKey.Trim();
            query = query.Where(e => e.IdempotencyKey == key);
        }

        if (descriptor.ProcessingStatus is { } processingStatus)
        {
            query = query.Where(e => e.ProcessingStatus == processingStatus);
        }

        return query.OrderByDescending(e => e.ReceivedAt);
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
        => DatabaseExceptions.IsUniqueConstraintViolation(exception);
}
