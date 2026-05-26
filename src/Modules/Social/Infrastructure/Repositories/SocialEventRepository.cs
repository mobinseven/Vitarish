using Vitarish.Entities.Server.Data;
namespace Vitarish.Social.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class SocialEventRepository(SocialDbContext dbContext)
    : EntityRepository<SocialEvent, SocialEventDescriptor, SocialDbContext>(dbContext),
      IEntityRepository<SocialEvent, SocialEventDescriptor>
{
    protected override IQueryable<SocialEvent> Apply(IQueryable<SocialEvent> query, SocialEventDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ConnectionId is { } connectionId)
        {
            query = query.Where(e => e.ConnectionId == connectionId);
        }

        if (descriptor.RawEventId is { } rawEventId)
        {
            query = query.Where(e => e.RawEventId == rawEventId);
        }

        if (descriptor.EventType is { } eventType)
        {
            query = query.Where(e => e.EventType == eventType);
        }

        return query.OrderByDescending(e => e.OccurredAt);
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
        => DatabaseExceptions.IsUniqueConstraintViolation(exception);
}
