using Vitarish.Entities.Server.Models;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IReservationManager : IEntityService<ReservationReadDto, ReservationWriteDto, ReservationDescriptorDto>
{
    Task<EntityResult> ReserveAsync(Guid sessionOccurrenceId, string? reservingPartyId, CancellationToken cancellationToken);
    Task<EntityResult> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> RejectReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> ExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid newSessionOccurrenceId, CancellationToken cancellationToken);
}
