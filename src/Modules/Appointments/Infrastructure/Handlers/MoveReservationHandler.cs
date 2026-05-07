using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class MoveReservationHandler(
    IEntityRepository<Reservation, ReservationDescriptor> repository,
    IAppointmentsEventPublisher eventPublisher)
    : IRequestHandler<MoveReservationCommand>
{
    public async Task Handle(MoveReservationCommand request, CancellationToken cancellationToken)
    {
        Reservation? entity = await repository.GetAsync(request.ReservationId, cancellationToken);

        if (entity is null || entity.SessionOccurrenceId == request.NewSessionOccurrenceId)
        {
            return;
        }

        Guid previousOccurrenceId = entity.SessionOccurrenceId;
        entity.SessionOccurrenceId = request.NewSessionOccurrenceId;
        _ = await repository.UpdateAsync(entity, cancellationToken);

        await eventPublisher.PublishReservationMovedAsync(
            new ReservationMovedEvent(entity.Id, previousOccurrenceId, entity.SessionOccurrenceId, entity.ReservingPartyId),
            cancellationToken);
    }
}
