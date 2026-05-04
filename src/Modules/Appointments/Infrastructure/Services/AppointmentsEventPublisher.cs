using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class AppointmentsEventPublisher(IPublisher publisher) : IAppointmentsEventPublisher
{
    public Task PublishReservationCreatedAsync(ReservationCreatedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishReservationConfirmedAsync(ReservationConfirmedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishReservationCancelledAsync(ReservationCancelledEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishReservationRejectedAsync(ReservationRejectedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishReservationExpiredAsync(ReservationExpiredEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishReservationMovedAsync(ReservationMovedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishSessionOpenedAsync(SessionOpenedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishSessionClosedAsync(SessionClosedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishSessionCancelledAsync(SessionCancelledEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishSessionArchivedAsync(SessionArchivedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);

    public Task PublishCapacityChangedAsync(CapacityChangedEvent @event, CancellationToken cancellationToken)
        => publisher.Publish(@event, cancellationToken);
}
