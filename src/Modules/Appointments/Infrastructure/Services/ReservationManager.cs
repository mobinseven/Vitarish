using Vitarish.Appointments.Domain.Mappers;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ReservationManager(IMediator mediator)
    : EntityService<Reservation, ReservationDescriptor, ReservationReadDto, ReservationWriteDto, ReservationDescriptorDto>(
        mediator,
        reservation => reservation.ToRead(),
        reservation => reservation.ToEntity(),
        reservation => reservation.ToDescriptor()),
      IReservationManager
{
    public async Task<EntityResult<ReservationReadDto>> ReserveAsync(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken)
        => await ReserveAsyncCore(shiftId, date, reservingPartyId, capacityOverride: false, estimatedSessionLength, cancellationToken);

    public async Task<EntityResult<ReservationReadDto>> ReserveWithOverrideAsync(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken)
        => await ReserveAsyncCore(shiftId, date, reservingPartyId, capacityOverride: true, estimatedSessionLength, cancellationToken);

    private async Task<EntityResult<ReservationReadDto>> ReserveAsyncCore(
        Guid shiftId,
        DateOnly date,
        string? reservingPartyId,
        bool capacityOverride,
        TimeSpan? estimatedSessionLength,
        CancellationToken cancellationToken)
    {
        EntityResult<Guid> reservationResult = await Mediator.Send(
            new CreateReservationCommand(
                shiftId,
                date,
                reservingPartyId,
                CapacityOverride: capacityOverride,
                EstimatedSessionLength: estimatedSessionLength),
            cancellationToken);
        return reservationResult.IsFailure
            ? EntityResult<ReservationReadDto>.Failure([.. reservationResult.Errors])
            : await GetAsync(reservationResult.Value, cancellationToken);
    }

    public Task<EntityResult> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => UpdateStatusAsync(reservationId, ReservationStatus.Confirmed, cancellationToken);

    public Task<EntityResult> PresentReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => UpdateStatusAsync(reservationId, ReservationStatus.Present, cancellationToken);

    public Task<EntityResult> CompleteReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => UpdateStatusAsync(reservationId, ReservationStatus.Completed, cancellationToken);

    public Task<EntityResult> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => UpdateStatusAsync(reservationId, ReservationStatus.Cancelled, cancellationToken);

    public Task<EntityResult> RejectReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => UpdateStatusAsync(reservationId, ReservationStatus.Rejected, cancellationToken);

    public Task<EntityResult> ExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken)
        => UpdateStatusAsync(reservationId, ReservationStatus.Expired, cancellationToken);

    public async Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid targetShiftId, DateOnly targetDate, CancellationToken cancellationToken)
    {
        await Mediator.Send(new MoveReservationCommand(reservationId, targetShiftId, targetDate), cancellationToken);
        return EntityResult.Success();
    }

    private async Task<EntityResult> UpdateStatusAsync(Guid reservationId, ReservationStatus status, CancellationToken cancellationToken)
    {
        await Mediator.Send(new UpdateReservationStatusCommand(reservationId, status), cancellationToken);
        return EntityResult.Success();
    }
}
