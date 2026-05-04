namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class UpdateReservationStatusHandler(IEntityRepository<Reservation, ReservationDescriptor> repository)
    : IRequestHandler<UpdateReservationStatusCommand>
{
    public async Task Handle(UpdateReservationStatusCommand request, CancellationToken cancellationToken)
    {
        Reservation? entity = await repository.GetAsync(request.ReservationId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = request.Status;

        if (request.Status == ReservationStatus.Confirmed)
        {
            entity.ConfirmedAt = DateTimeOffset.UtcNow;
            entity.IsHold = false;
            entity.ExpiresAt = null;
        }
        else if (request.Status == ReservationStatus.Cancelled)
        {
            entity.CancelledAt = DateTimeOffset.UtcNow;
        }

        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}
