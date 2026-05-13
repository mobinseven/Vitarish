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
    Task PublishShiftOpenedAsync(ShiftOpenedEvent @event, CancellationToken cancellationToken);
    Task PublishShiftClosedAsync(ShiftClosedEvent @event, CancellationToken cancellationToken);
    Task PublishShiftCancelledAsync(ShiftCancelledEvent @event, CancellationToken cancellationToken);
    Task PublishShiftArchivedAsync(ShiftArchivedEvent @event, CancellationToken cancellationToken);
    Task PublishCapacityChangedAsync(CapacityChangedEvent @event, CancellationToken cancellationToken);
}
