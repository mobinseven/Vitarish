using Vitarish.Appointments.Application.Events;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class StatusCoordinator(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<Reservation, ReservationDescriptor> reservationRepository,
    IAppointmentsEventPublisher eventPublisher,
    IAuditWriter auditWriter,
    IRequestContext requestContext) : IStatusCoordinator
{
    private static readonly Dictionary<SessionStatus, SessionStatus[]> SessionTransitions = new()
    {
        [SessionStatus.Draft] = [SessionStatus.Active, SessionStatus.Cancelled],
        [SessionStatus.Active] = [SessionStatus.Paused, SessionStatus.Closed, SessionStatus.Cancelled],
        [SessionStatus.Paused] = [SessionStatus.Active, SessionStatus.Closed, SessionStatus.Cancelled],
        [SessionStatus.Closed] = [SessionStatus.Archived],
        [SessionStatus.Cancelled] = [SessionStatus.Archived],
        [SessionStatus.Archived] = []
    };

    private static readonly Dictionary<ReservationStatus, ReservationStatus[]> ReservationTransitions = new()
    {
        [ReservationStatus.Pending] = [ReservationStatus.Confirmed, ReservationStatus.Cancelled, ReservationStatus.Rejected, ReservationStatus.Expired, ReservationStatus.Waitlisted],
        [ReservationStatus.Confirmed] = [ReservationStatus.Cancelled, ReservationStatus.Present],
        [ReservationStatus.Waitlisted] = [ReservationStatus.Pending, ReservationStatus.Cancelled],
        [ReservationStatus.Present] = [ReservationStatus.Completed],
        [ReservationStatus.Cancelled] = [],
        [ReservationStatus.Rejected] = [],
        [ReservationStatus.Expired] = [],
        [ReservationStatus.Completed] = []
    };

    public async Task<EntityResult> TransitionSessionAsync(Guid sessionId, SessionStatus newStatus, string? actorId, CancellationToken cancellationToken)
    {
        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Session not found.", nameof(sessionId)));
        }

        if (!SessionTransitions.TryGetValue(session.Status, out SessionStatus[]? allowed) || !allowed.Contains(newStatus))
        {
            return EntityResult.Failure(EntityError.Conflict($"Cannot transition session from {session.Status} to {newStatus}."));
        }

        SessionStatus previousStatus = session.Status;
        session.Status = newStatus;
        _ = await sessionRepository.UpdateAsync(session, cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = $"SessionStatus:{previousStatus}→{newStatus}",
            ActorId = actorId,
            EntityType = nameof(Session),
            EntityId = sessionId,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.")
        }, cancellationToken);

        await PublishSessionEventAsync(sessionId, newStatus, cancellationToken);
        return EntityResult.Success();
    }

    public async Task<EntityResult> TransitionReservationAsync(Guid reservationId, ReservationStatus newStatus, string? actorId, CancellationToken cancellationToken)
    {
        Reservation? reservation = await reservationRepository.GetAsync(reservationId, cancellationToken);
        if (reservation is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Reservation not found.", nameof(reservationId)));
        }

        if (!ReservationTransitions.TryGetValue(reservation.Status, out ReservationStatus[]? allowed) || !allowed.Contains(newStatus))
        {
            return EntityResult.Failure(EntityError.Conflict($"Cannot transition reservation from {reservation.Status} to {newStatus}."));
        }

        ReservationStatus previousStatus = reservation.Status;
        reservation.Status = newStatus;

        if (newStatus == ReservationStatus.Confirmed)
        {
            reservation.ConfirmedAt = DateTimeOffset.UtcNow;
            reservation.IsHold = false;
            reservation.ExpiresAt = null;
        }
        else if (newStatus == ReservationStatus.Cancelled)
        {
            reservation.CancelledAt = DateTimeOffset.UtcNow;
        }

        _ = await reservationRepository.UpdateAsync(reservation, cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = $"ReservationStatus:{previousStatus}→{newStatus}",
            ActorId = actorId,
            EntityType = nameof(Reservation),
            EntityId = reservationId,
            OccurredAt = DateTimeOffset.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID can not be recognized.")
        }, cancellationToken);

        await PublishReservationEventAsync(reservation, newStatus, cancellationToken);
        return EntityResult.Success();
    }

    private Task PublishSessionEventAsync(Guid sessionId, SessionStatus newStatus, CancellationToken cancellationToken) =>
        newStatus switch
        {
            SessionStatus.Active => eventPublisher.PublishSessionOpenedAsync(new SessionOpenedEvent(sessionId), cancellationToken),
            SessionStatus.Closed => eventPublisher.PublishSessionClosedAsync(new SessionClosedEvent(sessionId), cancellationToken),
            SessionStatus.Cancelled => eventPublisher.PublishSessionCancelledAsync(new SessionCancelledEvent(sessionId), cancellationToken),
            SessionStatus.Archived => eventPublisher.PublishSessionArchivedAsync(new SessionArchivedEvent(sessionId), cancellationToken),
            _ => Task.CompletedTask
        };

    private Task PublishReservationEventAsync(Reservation reservation, ReservationStatus newStatus, CancellationToken cancellationToken) =>
        newStatus switch
        {
            ReservationStatus.Confirmed => eventPublisher.PublishReservationConfirmedAsync(
                new ReservationConfirmedEvent(reservation.Id, reservation.SessionOccurrenceId, reservation.ReservingPartyId), cancellationToken),
            ReservationStatus.Cancelled => eventPublisher.PublishReservationCancelledAsync(
                new ReservationCancelledEvent(reservation.Id, reservation.SessionOccurrenceId, reservation.ReservingPartyId), cancellationToken),
            ReservationStatus.Rejected => eventPublisher.PublishReservationRejectedAsync(
                new ReservationRejectedEvent(reservation.Id, reservation.SessionOccurrenceId, reservation.ReservingPartyId), cancellationToken),
            ReservationStatus.Expired => eventPublisher.PublishReservationExpiredAsync(
                new ReservationExpiredEvent(reservation.Id, reservation.SessionOccurrenceId, reservation.ReservingPartyId), cancellationToken),
            _ => Task.CompletedTask
        };
}
