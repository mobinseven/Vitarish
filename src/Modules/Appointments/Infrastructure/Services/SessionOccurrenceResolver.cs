namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceResolver(
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    AppointmentsDbContext dbContext) : ISessionOccurrenceResolver
{
    public async Task<IReadOnlyList<SessionOccurrenceReadDto>> ResolveOccurrencesAsync(
        Guid sessionId, SessionDateRangeQuery range, CancellationToken cancellationToken)
    {
        if (!TryNormalizeRange(range, out DateOnly dateFrom, out DateOnly dateTo))
        {
            return [];
        }

        SessionOccurrenceDescriptor descriptor = new()
        {
            SessionId = sessionId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionOccurrence> occurrences = await occurrenceRepository.GetAsync(descriptor, cancellationToken);
        return [.. occurrences.Select(o => o.ToDto())];
    }

    public async Task<SessionOccurrenceReadDto?> GetOccurrenceAsync(
        Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .Include(o => o.Session)
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == date, cancellationToken);

        return occurrence?.ToDto();
    }

    public Task SyncOccurrencesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static bool TryNormalizeRange(SessionDateRangeQuery range, out DateOnly dateFrom, out DateOnly dateTo)
    {
        dateFrom = range.DateFrom;
        dateTo = range.DateTo;

        return dateFrom != default && dateTo != default && dateTo >= dateFrom;
    }
}
