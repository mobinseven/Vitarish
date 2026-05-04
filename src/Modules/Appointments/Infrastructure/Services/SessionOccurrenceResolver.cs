using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceResolver(
    ICalendarGateway calendarGateway,
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    AppointmentsDbContext dbContext) : ISessionOccurrenceResolver
{
    public async Task<IReadOnlyList<SessionOccurrenceReadDto>> ResolveOccurrencesAsync(
        Guid sessionId, TimeRangeQuery range, CancellationToken cancellationToken)
    {
        await SyncOccurrencesAsync(sessionId, cancellationToken);

        SessionOccurrenceDescriptor descriptor = new()
        {
            SessionId = sessionId,
            DateFrom = range.Start,
            DateTo = range.End,
            IsCancelled = false,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionOccurrence> occurrences = await occurrenceRepository.GetAsync(descriptor, cancellationToken);
        return [.. occurrences.Select(o => o.ToDto())];
    }

    public async Task<SessionOccurrenceReadDto?> GetOccurrenceAsync(
        Guid sessionId, string recurrenceId, CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .Include(o => o.Session)
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.RecurrenceId == recurrenceId, cancellationToken);

        return occurrence?.ToDto();
    }

    public async Task SyncOccurrencesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);

        if (session?.CalendarMappingId is null)
        {
            return;
        }

        TimeRangeQuery range = new()
        {
            Start = DateTimeOffset.UtcNow,
            End = DateTimeOffset.UtcNow.AddMonths(3)
        };

        IReadOnlyList<(string RecurrenceId, DateTimeOffset Start, DateTimeOffset End)> calendarOccurrences =
            await calendarGateway.GetUpcomingOccurrencesAsync(session.CalendarMappingId.Value, range, cancellationToken);

        foreach ((string recurrenceId, DateTimeOffset start, DateTimeOffset end) in calendarOccurrences)
        {
            SessionOccurrence? existing = await dbContext.SessionOccurrences
                .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.RecurrenceId == recurrenceId, cancellationToken);

            if (existing is null)
            {
                dbContext.SessionOccurrences.Add(new SessionOccurrence
                {
                    SessionId = sessionId,
                    CalendarMappingId = session.CalendarMappingId.Value,
                    RecurrenceId = recurrenceId,
                    OccurrenceStart = start,
                    OccurrenceEnd = end
                });
            }
            else
            {
                existing.OccurrenceStart = start;
                existing.OccurrenceEnd = end;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
