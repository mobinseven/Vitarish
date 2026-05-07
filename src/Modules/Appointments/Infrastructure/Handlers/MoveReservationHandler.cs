namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class MoveReservationHandler(IEntityRepository<Reservation, ReservationDescriptor> repository)
    : IRequestHandler<MoveReservationCommand>
{
    public async Task Handle(MoveReservationCommand request, CancellationToken cancellationToken)
    {
        Reservation? entity = await repository.GetAsync(request.ReservationId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.SessionOccurrenceId = request.NewSessionOccurrenceId;
        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}
