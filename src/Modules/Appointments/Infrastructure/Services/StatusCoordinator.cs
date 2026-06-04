using Vitarish.Appointments.Application.Events;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class StatusCoordinator(
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IEntityRepository<Reservation, ReservationDescriptor> reservationRepository,
    IAppointmentsEventPublisher eventPublisher,
    IAuditWriter auditWriter,
    IRequestContext requestContext) : IStatusCoordinator
{
    private static readonly Dictionary<ShiftStatus, ShiftStatus[]> ShiftTransitions = new()
    {
        [ShiftStatus.Draft] = [ShiftStatus.Active, ShiftStatus.Cancelled],
        [ShiftStatus.Active] = [ShiftStatus.Paused, ShiftStatus.Closed, ShiftStatus.Cancelled],
        [ShiftStatus.Paused] = [ShiftStatus.Active, ShiftStatus.Closed, ShiftStatus.Cancelled],
        [ShiftStatus.Closed] = [ShiftStatus.Archived],
        [ShiftStatus.Cancelled] = [ShiftStatus.Archived],
        [ShiftStatus.Archived] = []
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

    public async Task<EntityResult> TransitionShiftAsync(Guid shiftId, ShiftStatus newStatus, string? actorId, CancellationToken cancellationToken)
    {
        Shift? shift = await shiftRepository.GetAsync(shiftId, cancellationToken);
        if (shift is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Shift not found.", nameof(shiftId)));
        }

        if (!ShiftTransitions.TryGetValue(shift.Status, out ShiftStatus[]? allowed) || !allowed.Contains(newStatus))
        {
            return EntityResult.Failure(EntityError.Conflict($"Cannot transition shift from {shift.Status} to {newStatus}."));
        }

        ShiftStatus previousStatus = shift.Status;
        shift.Status = newStatus;
        _ = await shiftRepository.UpdateAsync(shift, cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = $"ShiftStatus:{previousStatus}→{newStatus}",
            ActorId = actorId,
            EntityType = nameof(Shift),
            EntityId = shiftId,
            OccurredAt = DateTime.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
        }, cancellationToken);

        await PublishShiftEventAsync(shiftId, newStatus, cancellationToken);
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
            reservation.ConfirmedAt = DateTime.UtcNow;
            reservation.IsHold = false;
            reservation.ExpiresAt = null;
        }
        else if (newStatus == ReservationStatus.Cancelled)
        {
            reservation.CancelledAt = DateTime.UtcNow;
        }

        _ = await reservationRepository.UpdateAsync(reservation, cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Action = $"ReservationStatus:{previousStatus}→{newStatus}",
            ActorId = actorId,
            EntityType = nameof(Reservation),
            EntityId = reservationId,
            OccurredAt = DateTime.UtcNow,
            TenantId = requestContext.TenantId ?? throw new InvalidOperationException("Tenant ID cannot be recognized.")
        }, cancellationToken);

        await PublishReservationEventAsync(reservation, newStatus, cancellationToken);
        return EntityResult.Success();
    }

    private Task PublishShiftEventAsync(Guid shiftId, ShiftStatus newStatus, CancellationToken cancellationToken) =>
        newStatus switch
        {
            ShiftStatus.Active => eventPublisher.PublishShiftOpenedAsync(new ShiftOpenedEvent(shiftId), cancellationToken),
            ShiftStatus.Closed => eventPublisher.PublishShiftClosedAsync(new ShiftClosedEvent(shiftId), cancellationToken),
            ShiftStatus.Cancelled => eventPublisher.PublishShiftCancelledAsync(new ShiftCancelledEvent(shiftId), cancellationToken),
            ShiftStatus.Archived => eventPublisher.PublishShiftArchivedAsync(new ShiftArchivedEvent(shiftId), cancellationToken),
            _ => Task.CompletedTask
        };

    private Task PublishReservationEventAsync(Reservation reservation, ReservationStatus newStatus, CancellationToken cancellationToken) =>
        newStatus switch
        {
            ReservationStatus.Confirmed => eventPublisher.PublishReservationConfirmedAsync(
                new ReservationConfirmedEvent(reservation.Id, reservation.ShiftId, reservation.Date, reservation.ReservingPartyId), cancellationToken),
            ReservationStatus.Cancelled => eventPublisher.PublishReservationCancelledAsync(
                new ReservationCancelledEvent(reservation.Id, reservation.ShiftId, reservation.Date, reservation.ReservingPartyId), cancellationToken),
            ReservationStatus.Rejected => eventPublisher.PublishReservationRejectedAsync(
                new ReservationRejectedEvent(reservation.Id, reservation.ShiftId, reservation.Date, reservation.ReservingPartyId), cancellationToken),
            ReservationStatus.Expired => eventPublisher.PublishReservationExpiredAsync(
                new ReservationExpiredEvent(reservation.Id, reservation.ShiftId, reservation.Date, reservation.ReservingPartyId), cancellationToken),
            _ => Task.CompletedTask
        };
}
