namespace Vitarish.Appointments.Application.Abstractions;

public interface IReservationManager : IEntityService<ReservationReadDto, ReservationWriteDto, ReservationDescriptorDto>
{
    Task<EntityResult<ReservationReadDto>> ReserveAsync(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken);
    Task<EntityResult<ReservationReadDto>> ReserveWithOverrideAsync(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken);
    Task<EntityResult> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> PresentReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> CompleteReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> RejectReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> ExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid targetShiftId, DateOnly targetDate, CancellationToken cancellationToken);
}
