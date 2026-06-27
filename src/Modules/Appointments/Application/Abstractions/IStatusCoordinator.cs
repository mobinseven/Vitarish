using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IStatusCoordinator
{
    Task<EntityResult> TransitionShiftAsync(Guid shiftId, ShiftStatus newStatus, string? actorId, CancellationToken cancellationToken);
    Task<EntityResult> TransitionReservationAsync(Guid reservationId, ReservationStatus newStatus, string? actorId, CancellationToken cancellationToken);
}
