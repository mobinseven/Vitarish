using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class CreateReservationHandler(
    IEntityRepository<Reservation, ReservationDescriptor> reservationRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    IAppointmentsEventPublisher eventPublisher,
    IPublisher publisher)
    : IRequestHandler<CreateReservationCommand, Guid>
{
    public async Task<Guid> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        Reservation reservation = new()
        {
            SessionOccurrenceId = request.SessionOccurrenceId,
            ReservingPartyId = request.ReservingPartyId,
            Status = ReservationStatus.Pending,
            IsHold = request.IsHold,
            ExpiresAt = request.IsHold && request.HoldDuration.HasValue
                ? DateTimeOffset.UtcNow.Add(request.HoldDuration.Value)
                : null
        };

        Reservation created = await reservationRepository.AddAsync(reservation, cancellationToken);
        await eventPublisher.PublishReservationCreatedAsync(
            new ReservationCreatedEvent(created.Id, created.SessionOccurrenceId, created.ReservingPartyId),
            cancellationToken);

        SessionOccurrence? occurrence = await occurrenceRepository.GetAsync(request.SessionOccurrenceId, cancellationToken);
        if (occurrence is not null)
        {
            await publisher.Publish(
                new SessionReminderPolicyTriggeredEvent(
                    created.Id,
                    created.SessionOccurrenceId,
                    created.ReservingPartyId,
                    ReservationStatus.Pending,
                    occurrence.OccurrenceStart),
                cancellationToken);
        }

        return created.Id;
    }
}

