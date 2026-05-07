using Vitarish.Appointments.Application.Events;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal sealed class ReservationCreatedReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<ReservationCreatedEvent>
{
    public Task Handle(ReservationCreatedEvent notification, CancellationToken cancellationToken)
        => reminders.MaterializeAutoRemindersAsync(
            notification.ReservationId,
            notification.SessionOccurrenceId,
            notification.ReservingPartyId,
            cancellationToken);
}

internal sealed class ReservationConfirmedReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<ReservationConfirmedEvent>
{
    public Task Handle(ReservationConfirmedEvent notification, CancellationToken cancellationToken)
        => reminders.MaterializeAutoRemindersAsync(
            notification.ReservationId,
            notification.SessionOccurrenceId,
            notification.ReservingPartyId,
            cancellationToken);
}

internal sealed class ReservationCancelledReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<ReservationCancelledEvent>
{
    public Task Handle(ReservationCancelledEvent notification, CancellationToken cancellationToken)
        => reminders.RemoveUnsentAutoRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationRejectedReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<ReservationRejectedEvent>
{
    public Task Handle(ReservationRejectedEvent notification, CancellationToken cancellationToken)
        => reminders.RemoveUnsentAutoRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationExpiredReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<ReservationExpiredEvent>
{
    public Task Handle(ReservationExpiredEvent notification, CancellationToken cancellationToken)
        => reminders.RemoveUnsentAutoRemindersAsync(notification.ReservationId, cancellationToken);
}

internal sealed class ReservationMovedReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<ReservationMovedEvent>
{
    public Task Handle(ReservationMovedEvent notification, CancellationToken cancellationToken)
        => reminders.RecalculateUnsentAutoRemindersAsync(
            notification.ReservationId,
            notification.ToOccurrenceId,
            notification.ReservingPartyId,
            cancellationToken);
}

internal sealed class SessionCancelledReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<SessionCancelledEvent>
{
    public Task Handle(SessionCancelledEvent notification, CancellationToken cancellationToken)
        => reminders.RemoveUnsentAutoRemindersForSessionAsync(notification.SessionId, cancellationToken);
}

internal sealed class SessionClosedReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<SessionClosedEvent>
{
    public Task Handle(SessionClosedEvent notification, CancellationToken cancellationToken)
        => reminders.RemoveUnsentAutoRemindersForSessionAsync(notification.SessionId, cancellationToken);
}

internal sealed class SessionArchivedReminderHandler(IAppointmentReminderService reminders)
    : INotificationHandler<SessionArchivedEvent>
{
    public Task Handle(SessionArchivedEvent notification, CancellationToken cancellationToken)
        => reminders.RemoveUnsentAutoRemindersForSessionAsync(notification.SessionId, cancellationToken);
}
