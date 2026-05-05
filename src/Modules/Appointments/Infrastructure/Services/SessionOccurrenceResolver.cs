using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceResolver(
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

        if (session?.StartAt is null || session.EndAt is null)
        {
            return;
        }

        const string singleOccurrenceRecurrenceId = "single";

        SessionOccurrence? existing = await dbContext.SessionOccurrences
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.RecurrenceId == singleOccurrenceRecurrenceId, cancellationToken);

        if (existing is null)
        {
            dbContext.SessionOccurrences.Add(new SessionOccurrence
            {
                SessionId = sessionId,
                CalendarMappingId = session.CalendarMappingId ?? Guid.Empty,
                RecurrenceId = singleOccurrenceRecurrenceId,
                OccurrenceStart = session.StartAt.Value,
                OccurrenceEnd = session.EndAt.Value,
                IsCancelled = session.Status == SessionStatus.Cancelled
            });
        }
        else
        {
            existing.CalendarMappingId = session.CalendarMappingId ?? Guid.Empty;
            existing.OccurrenceStart = session.StartAt.Value;
            existing.OccurrenceEnd = session.EndAt.Value;
            existing.IsCancelled = session.Status == SessionStatus.Cancelled;
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
