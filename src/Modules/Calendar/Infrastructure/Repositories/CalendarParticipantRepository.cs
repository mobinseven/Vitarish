namespace Vitarish.Calendar.Infrastructure.Repositories;

[RegisterTransient]
internal class CalendarParticipantRepository(CalendarDbContext dbContext)
    : EntityRepository<CalendarParticipant, CalendarParticipantDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<CalendarParticipant, CalendarParticipantDescriptor>
{
    protected override IQueryable<CalendarParticipant> Apply(IQueryable<CalendarParticipant> query, CalendarParticipantDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.MappingId is { } mappingId)
        {
            query = query.Where(p => p.MappingId == mappingId);
        }

        if (descriptor.ParticipantType is { } participantType)
        {
            query = query.Where(p => p.ParticipantType == participantType);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
