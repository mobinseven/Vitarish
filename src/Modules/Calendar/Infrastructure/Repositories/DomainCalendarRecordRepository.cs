namespace Vitarish.Calendar.Infrastructure.Repositories;

[RegisterTransient]
internal class DomainCalendarRecordRepository(CalendarDbContext dbContext)
    : EntityRepository<DomainCalendarRecord, DomainCalendarRecordDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<DomainCalendarRecord, DomainCalendarRecordDescriptor>
{
    protected override IQueryable<DomainCalendarRecord> Apply(IQueryable<DomainCalendarRecord> query, DomainCalendarRecordDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Category is { } category)
        {
            query = query.Where(r => r.Category == category);
        }

        if (descriptor.Visibility is { } visibility)
        {
            query = query.Where(r => r.Visibility == visibility);
        }

        if (descriptor.OwnerId is { } ownerId)
        {
            query = query.Where(r => r.OwnerId == ownerId);
        }

        if (descriptor.ProviderId is { } providerId)
        {
            query = query.Where(r => r.ProviderId == providerId);
        }

        if (descriptor.PatientId is { } patientId)
        {
            query = query.Where(r => r.PatientId == patientId);
        }

        if (descriptor.BusinessStatus is { } businessStatus)
        {
            query = query.Where(r => r.BusinessStatus == businessStatus);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
