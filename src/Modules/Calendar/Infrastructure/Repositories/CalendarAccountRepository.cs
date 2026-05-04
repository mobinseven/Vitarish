namespace Vitarish.Calendar.Infrastructure.Repositories;

[RegisterTransient]
internal class CalendarAccountRepository(CalendarDbContext dbContext)
    : EntityRepository<CalendarAccount, CalendarAccountDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<CalendarAccount, CalendarAccountDescriptor>
{
    protected override IQueryable<CalendarAccount> Apply(IQueryable<CalendarAccount> query, CalendarAccountDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.LocalEntityType is { } entityType)
        {
            query = query.Where(a => a.LocalEntityType == entityType);
        }

        if (descriptor.LocalEntityId is { } localId)
        {
            query = query.Where(a => a.LocalEntityId == localId);
        }

        if (descriptor.IsActive is { } isActive)
        {
            query = query.Where(a => a.IsActive == isActive);
        }

        if (descriptor.SyncStatus is { } syncStatus)
        {
            query = query.Where(a => a.SyncStatus == syncStatus);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
