namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionsService(IMediator mediator, AppointmentsDbContext dbContext)
    : EntityService<Session, SessionDescriptor, SessionReadDto, SessionWriteDto, SessionDescriptorDto>(
        mediator,
        session => session.ToDto(),
        session => session.ToDomain(),
        session => session.ToDomain()),
      ISessionsService
{
    // ── Override GetAsync to enrich with ReservationCount ──────────────────────

    async Task<EntityResult<SessionReadDto>> IEntityService<SessionReadDto, SessionWriteDto, SessionDescriptorDto>.GetAsync(
        Guid id, CancellationToken cancellationToken)
    {
        EntityResult<SessionReadDto> result = await base.GetAsync(id, cancellationToken);
        if (!result.IsFailure && result.Value is not null)
        {
            result.Value.ReservationCount = await CountReservationsAsync(id, cancellationToken);
        }

        return result;
    }

    // ── Override GetListAsync to enrich each session with ReservationCount ─────

    async Task<EntityResult<IReadOnlyList<SessionReadDto>>> IEntityService<SessionReadDto, SessionWriteDto, SessionDescriptorDto>.GetListAsync(
        SessionDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<SessionReadDto>> result = await base.GetListAsync(descriptor, cancellationToken);
        if (!result.IsFailure && result.Value is { Count: > 0 })
        {
            await EnrichWithReservationCountsAsync(result.Value, cancellationToken);
        }

        return result;
    }

    // ── Custom operations ───────────────────────────────────────────────────────

    public async Task<EntityResult<int>> GetAvailableCapacityAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return EntityResult<int>.Failure(EntityError.NotFound("Session not found.", nameof(sessionId)));
        }

        int reservationCount = await CountReservationsAsync(sessionId, cancellationToken);
        return EntityResult<int>.Success(session.Capacity - reservationCount);
    }

    public async Task<EntityResult> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new CancelSessionCommand(id), cancellationToken);
        return EntityResult.Success();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Task<int> CountReservationsAsync(Guid sessionId, CancellationToken cancellationToken)
        => dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId && a.Status != AppointmentStatus.Cancelled)
            .CountAsync(cancellationToken);

    private async Task EnrichWithReservationCountsAsync(
        IReadOnlyList<SessionReadDto> sessions, CancellationToken cancellationToken)
    {
        Guid[] sessionIds = [.. sessions.Select(s => s.Id)];

        var counts = await dbContext.Appointments
            .AsNoTracking()
            .Where(a => sessionIds.Contains(a.SessionId!.Value) && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.SessionId!.Value)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (SessionReadDto session in sessions)
        {
            session.ReservationCount = counts.FirstOrDefault(c => c.SessionId == session.Id)?.Count ?? 0;
        }
    }
}
