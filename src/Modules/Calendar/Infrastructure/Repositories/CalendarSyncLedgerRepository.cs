namespace Vitarish.Calendar.Infrastructure.Repositories;

[RegisterTransient]
internal class CalendarSyncLedgerRepository(CalendarDbContext dbContext)
    : EntityRepository<CalendarSyncLedger, CalendarSyncLedgerDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<CalendarSyncLedger, CalendarSyncLedgerDescriptor>
{
    protected override IQueryable<CalendarSyncLedger> Apply(IQueryable<CalendarSyncLedger> query, CalendarSyncLedgerDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.CalendarAccountId is { } accountId)
        {
            query = query.Where(l => l.CalendarAccountId == accountId);
        }

        if (descriptor.Status is { } status)
        {
            query = query.Where(l => l.Status == status);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
