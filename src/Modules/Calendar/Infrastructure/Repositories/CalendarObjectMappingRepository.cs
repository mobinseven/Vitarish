namespace Vitarish.Calendar.Infrastructure.Repositories;

[RegisterTransient]
internal class CalendarObjectMappingRepository(CalendarDbContext dbContext)
    : EntityRepository<CalendarObjectMapping, CalendarObjectMappingDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<CalendarObjectMapping, CalendarObjectMappingDescriptor>
{
    protected override IQueryable<CalendarObjectMapping> Apply(IQueryable<CalendarObjectMapping> query, CalendarObjectMappingDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.CalendarAccountId is { } accountId)
        {
            query = query.Where(m => m.CalendarAccountId == accountId);
        }

        if (descriptor.Category is { } category)
        {
            query = query.Where(m => m.Category == category);
        }

        if (descriptor.ICalendarUid is { } uid)
        {
            query = query.Where(m => m.ICalendarUid == uid);
        }

        if (descriptor.IsExternallyManaged is { } external)
        {
            query = query.Where(m => m.IsExternallyManaged == external);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
