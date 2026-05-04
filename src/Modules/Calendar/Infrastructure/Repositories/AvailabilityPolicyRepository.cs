namespace Vitarish.Calendar.Infrastructure.Repositories;

[RegisterTransient]
internal class AvailabilityPolicyRepository(CalendarDbContext dbContext)
    : EntityRepository<AvailabilityPolicy, AvailabilityPolicyDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<AvailabilityPolicy, AvailabilityPolicyDescriptor>
{
    protected override IQueryable<AvailabilityPolicy> Apply(IQueryable<AvailabilityPolicy> query, AvailabilityPolicyDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.CalendarAccountId is { } accountId)
        {
            query = query.Where(p => p.CalendarAccountId == accountId);
        }

        if (descriptor.Category is { } category)
        {
            query = query.Where(p => p.Category == category);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
