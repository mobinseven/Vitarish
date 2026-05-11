namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ReservationManager(IMediator mediator)
    : EntityService<Reservation, ReservationDescriptor, ReservationReadDto, ReservationWriteDto, ReservationDescriptorDto>(
        mediator,
        reservation => reservation.ToDto(),
        reservation => reservation.ToDomain(),
        reservation => reservation.ToDomain()),
      IReservationManager
{
    public async Task<EntityResult<ReservationReadDto>> ReserveAsync(
        Guid sessionId,
        DateOnly date,
        string? reservingPartyId,
        CancellationToken cancellationToken)
    {
        EntityResult<Guid> reservationResult = await Mediator.Send(
            new CreateReservationCommand(sessionId, date, reservingPartyId),
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

    public async Task<EntityResult> MoveReservationAsync(Guid reservationId, Guid targetSessionId, DateOnly targetDate, CancellationToken cancellationToken)
    {
        await Mediator.Send(new MoveReservationCommand(reservationId, targetSessionId, targetDate), cancellationToken);
        return EntityResult.Success();
    }

    private async Task<EntityResult> UpdateStatusAsync(Guid reservationId, ReservationStatus status, CancellationToken cancellationToken)
    {
        await Mediator.Send(new UpdateReservationStatusCommand(reservationId, status), cancellationToken);
        return EntityResult.Success();
    }
}
