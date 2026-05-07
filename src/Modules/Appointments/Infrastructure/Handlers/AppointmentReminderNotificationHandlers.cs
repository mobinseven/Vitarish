using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal sealed class ReservationCreatedReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<ReservationCreatedEvent>
{
    public async Task Handle(ReservationCreatedEvent notification, CancellationToken cancellationToken)
        => await reminderService.MaterializeAutoRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationConfirmedReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<ReservationConfirmedEvent>
{
    public async Task Handle(ReservationConfirmedEvent notification, CancellationToken cancellationToken)
        => await reminderService.MaterializeAutoRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationCancelledReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<ReservationCancelledEvent>
{
    public async Task Handle(ReservationCancelledEvent notification, CancellationToken cancellationToken)
        => await reminderService.RemoveUnsentReservationRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationRejectedReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<ReservationRejectedEvent>
{
    public async Task Handle(ReservationRejectedEvent notification, CancellationToken cancellationToken)
        => await reminderService.RemoveUnsentReservationRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationExpiredReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<ReservationExpiredEvent>
{
    public async Task Handle(ReservationExpiredEvent notification, CancellationToken cancellationToken)
        => await reminderService.RemoveUnsentReservationRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationMovedReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<ReservationMovedEvent>
{
    public async Task Handle(ReservationMovedEvent notification, CancellationToken cancellationToken)
        => await reminderService.RecalculateUnsentReservationRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class SessionCancelledReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<SessionCancelledEvent>
{
    public async Task Handle(SessionCancelledEvent notification, CancellationToken cancellationToken)
        => await reminderService.RemoveUnsentSessionRemindersAsync(notification.SessionId, cancellationToken);
}

internal sealed class SessionClosedReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<SessionClosedEvent>
{
    public async Task Handle(SessionClosedEvent notification, CancellationToken cancellationToken)
        => await reminderService.RemoveUnsentSessionRemindersAsync(notification.SessionId, cancellationToken);
}

internal sealed class SessionArchivedReminderHandler(IAppointmentReminderService reminderService)
    : INotificationHandler<SessionArchivedEvent>
{
    public async Task Handle(SessionArchivedEvent notification, CancellationToken cancellationToken)
        => await reminderService.RemoveUnsentSessionRemindersAsync(notification.SessionId, cancellationToken);
}
