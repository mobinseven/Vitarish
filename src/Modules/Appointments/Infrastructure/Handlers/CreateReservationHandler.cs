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
        SessionOccurrence occurrence;
        if (request.SessionOccurrenceId is { } existingOccurrenceId)
        {
            SessionOccurrence? existingOccurrence = await occurrenceRepository.GetAsync(existingOccurrenceId, cancellationToken);
            if (existingOccurrence is null)
            {
                return EntityResult<Guid>.Failure(EntityError.NotFound("Session occurrence not found.", nameof(request.SessionOccurrenceId)));
            }

            occurrence = existingOccurrence;
        }
        else
        {
            EntityResult<SessionOccurrence> occurrenceResult = await occurrenceFactory.GetOrCreateOccurrenceAsync(
                request.SessionId,
                request.Date,
                cancellationToken);

            if (occurrenceResult.IsFailure)
            {
                return EntityResult<Guid>.Failure([.. occurrenceResult.Errors]);
            }

            if (occurrenceResult.Value is null)
            {
                return EntityResult<Guid>.Failure(EntityError.Conflict("Session occurrence could not be created."));
            }

            occurrence = occurrenceResult.Value;
        }

        Guid sessionOccurrenceId = occurrence.Id;
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

        await publisher.Publish(
            new SessionReminderPolicyTriggeredEvent(
                created.Id,
                created.SessionOccurrenceId,
                created.ReservingPartyId,
                ReservationStatus.Pending,
                new DateTimeOffset(occurrence.Date.ToDateTime(occurrence.StartTime), TimeSpan.Zero)),
            cancellationToken);

        return EntityResult<Guid>.Success(created.Id);
    }
}
