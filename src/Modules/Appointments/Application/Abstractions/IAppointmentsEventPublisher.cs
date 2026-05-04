using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IAppointmentsEventPublisher
{
    Task PublishReservationCreatedAsync(ReservationCreatedEvent @event, CancellationToken cancellationToken);
    Task PublishReservationConfirmedAsync(ReservationConfirmedEvent @event, CancellationToken cancellationToken);
    Task PublishReservationCancelledAsync(ReservationCancelledEvent @event, CancellationToken cancellationToken);
    Task PublishReservationRejectedAsync(ReservationRejectedEvent @event, CancellationToken cancellationToken);
    Task PublishReservationExpiredAsync(ReservationExpiredEvent @event, CancellationToken cancellationToken);
    Task PublishReservationMovedAsync(ReservationMovedEvent @event, CancellationToken cancellationToken);
    Task PublishSessionOpenedAsync(SessionOpenedEvent @event, CancellationToken cancellationToken);
    Task PublishSessionClosedAsync(SessionClosedEvent @event, CancellationToken cancellationToken);
    Task PublishSessionCancelledAsync(SessionCancelledEvent @event, CancellationToken cancellationToken);
    Task PublishSessionArchivedAsync(SessionArchivedEvent @event, CancellationToken cancellationToken);
    Task PublishCapacityChangedAsync(CapacityChangedEvent @event, CancellationToken cancellationToken);
}
