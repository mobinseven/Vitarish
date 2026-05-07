namespace Vitarish.Appointments.Application.Abstractions;

public interface IAppointmentReminderService
{
    Task MaterializeAutoRemindersAsync(
        Guid reservationId,
        Guid sessionOccurrenceId,
        string? reservingPartyId,
        CancellationToken cancellationToken);

    Task RemoveUnsentAutoRemindersAsync(
        Guid reservationId,
        CancellationToken cancellationToken);

    Task RecalculateUnsentAutoRemindersAsync(
        Guid reservationId,
        Guid sessionOccurrenceId,
        string? reservingPartyId,
        CancellationToken cancellationToken);

    Task RemoveUnsentAutoRemindersForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken);
}
