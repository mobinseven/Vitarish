using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class UpdateReservationStatusHandler(
    IEntityRepository<Reservation, ReservationDescriptor> repository,
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IPublisher publisher)
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
            entity.ConfirmedAt = DateTime.UtcNow;
            entity.IsHold = false;
            entity.ExpiresAt = null;
        }
        else if (request.Status == ReservationStatus.Cancelled)
        {
            entity.CancelledAt = DateTime.UtcNow;
        }

        _ = await repository.UpdateAsync(entity, cancellationToken);

        Shift? shift = await shiftRepository.GetAsync(entity.ShiftId, cancellationToken);
        if (shift is not null)
        {
            await publisher.Publish(
                new ShiftReminderPolicyTriggeredEvent(
                    entity.Id,
                    entity.ShiftId,
                    entity.Date,
                    entity.ReservingPartyId,
                    request.Status),
                cancellationToken);
        }
    }
}

