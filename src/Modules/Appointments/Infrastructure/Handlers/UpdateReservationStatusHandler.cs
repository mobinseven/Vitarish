using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class UpdateReservationStatusHandler(
    IEntityRepository<Reservation, ReservationDescriptor> repository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
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
            entity.ConfirmedAt = DateTimeOffset.UtcNow;
            entity.IsHold = false;
            entity.ExpiresAt = null;
        }
        else if (request.Status == ReservationStatus.Cancelled)
        {
            entity.CancelledAt = DateTimeOffset.UtcNow;
        }

        _ = await repository.UpdateAsync(entity, cancellationToken);

        SessionOccurrence? occurrence = await occurrenceRepository.GetAsync(entity.SessionOccurrenceId, cancellationToken);
        if (occurrence is not null)
        {
            await publisher.Publish(
                new SessionReminderPolicyTriggeredEvent(
                    entity.Id,
                    entity.SessionOccurrenceId,
                    entity.ReservingPartyId,
                    request.Status,
                    occurrence.OccurrenceStart),
                cancellationToken);
        }
    }
}

