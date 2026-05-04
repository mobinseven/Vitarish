using Vitarish.Entities.Server.Models;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IStatusCoordinator
{
    Task<EntityResult> TransitionSessionAsync(Guid sessionId, SessionStatus newStatus, string? actorId, CancellationToken cancellationToken);
    Task<EntityResult> TransitionReservationAsync(Guid reservationId, ReservationStatus newStatus, string? actorId, CancellationToken cancellationToken);
}
