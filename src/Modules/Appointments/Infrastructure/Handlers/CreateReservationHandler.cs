using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class CreateReservationHandler(
    IEntityRepository<Reservation, ReservationDescriptor> reservationRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    ISessionOccurrenceFactory occurrenceFactory,
    IAppointmentsEventPublisher eventPublisher,
    IPublisher publisher)
    : IRequestHandler<CreateReservationCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        EntityResult<Guid> occurrenceResult = await occurrenceFactory.GetOrCreateAsync(
            request.SessionId,
            request.Date,
            cancellationToken);

        if (occurrenceResult.IsFailure)
        {
            return EntityResult<Guid>.Failure([.. occurrenceResult.Errors]);
        }

        Guid sessionOccurrenceId = occurrenceResult.Value;
        Reservation reservation = new()
        {
            SessionOccurrenceId = sessionOccurrenceId,
            ReservingPartyId = request.ReservingPartyId,
            Status = ReservationStatus.Pending,
            IsHold = request.IsHold,
            ExpiresAt = request.IsHold && request.HoldDuration.HasValue
                ? DateTimeOffset.UtcNow.Add(request.HoldDuration.Value)
                : null
        };

        Reservation? created = await reservationRepository.AddAsync(reservation, cancellationToken);
        if (created is null)
        {
            return EntityResult<Guid>.Failure(EntityError.Unknown("Reservation could not be created."));
        }

        await eventPublisher.PublishReservationCreatedAsync(
            new ReservationCreatedEvent(created.Id, created.SessionOccurrenceId, created.ReservingPartyId),
            cancellationToken);

        SessionOccurrence? occurrence = await occurrenceRepository.GetAsync(sessionOccurrenceId, cancellationToken);
        if (occurrence is not null)
        {
            await publisher.Publish(
                new SessionReminderPolicyTriggeredEvent(
                    created.Id,
                    created.SessionOccurrenceId,
                    created.ReservingPartyId,
                    ReservationStatus.Pending,
                    new DateTimeOffset(occurrence.Date.ToDateTime(occurrence.StartTime), TimeSpan.Zero)),
                cancellationToken);
        }

        return EntityResult<Guid>.Success(created.Id);
    }
}
