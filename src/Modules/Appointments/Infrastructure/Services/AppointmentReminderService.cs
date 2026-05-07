using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal sealed class AppointmentReminderService(
    AppointmentsDbContext dbContext,
    IRemindersService remindersService) : IAppointmentReminderService
{
    private const string MarkerPrefix = "appointment-auto-reminder";
    private static readonly IReadOnlyList<AutoReminderPolicy> Policies =
    [
        new("appointment-start-24h", TimeSpan.FromHours(24), ReminderKind.Email)
    ];

    public async Task MaterializeAutoRemindersAsync(
        Guid reservationId,
        Guid sessionOccurrenceId,
        string? reservingPartyId,
        CancellationToken cancellationToken)
    {
        if (reservationId == Guid.Empty || sessionOccurrenceId == Guid.Empty || string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return;
        }

        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == sessionOccurrenceId, cancellationToken);

        if (occurrence is null)
        {
            return;
        }

        IReadOnlyList<ReminderReadDto> existing = await remindersService.GetBySourceAsync(reservationId, cancellationToken);

        foreach (AutoReminderPolicy policy in Policies)
        {
            DateTimeOffset remindAt = occurrence.OccurrenceStart - policy.OffsetBeforeStart;
            string marker = CreateMarker(policy.Id, sessionOccurrenceId);

            if (existing.Any(reminder => IsAutoReminder(reminder, marker)))
            {
                continue;
            }

            string body = CreateBody(occurrence.OccurrenceStart, marker);
            await remindersService.AddRangeAsync(
                reservationId,
                [new ReminderRecipientReadDto
                {
                    Kind = policy.Kind,
                    RecipientUserId = reservingPartyId,
                    Recipient = reservingPartyId
                }],
                "Appointment reminder",
                body,
                remindAt,
                cancellationToken);
        }
    }

    public async Task RemoveUnsentAutoRemindersAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        if (reservationId == Guid.Empty)
        {
            return;
        }

        IReadOnlyList<ReminderReadDto> reminders = await remindersService.GetBySourceAsync(reservationId, cancellationToken);
        foreach (ReminderReadDto reminder in reminders.Where(IsUnsentAutoReminder))
        {
            _ = await remindersService.RemoveAsync(reminder.Id, cancellationToken);
        }
    }

    public async Task RecalculateUnsentAutoRemindersAsync(
        Guid reservationId,
        Guid sessionOccurrenceId,
        string? reservingPartyId,
        CancellationToken cancellationToken)
    {
        await RemoveUnsentAutoRemindersAsync(reservationId, cancellationToken);
        await MaterializeAutoRemindersAsync(reservationId, sessionOccurrenceId, reservingPartyId, cancellationToken);
    }

    public async Task RemoveUnsentAutoRemindersForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty)
        {
            return;
        }

        List<Guid> reservationIds = await (from reservation in dbContext.Reservations.AsNoTracking()
                                           join occurrence in dbContext.SessionOccurrences.AsNoTracking()
                                               on reservation.SessionOccurrenceId equals occurrence.Id
                                           where occurrence.SessionId == sessionId
                                           select reservation.Id)
            .ToListAsync(cancellationToken);

        foreach (Guid reservationId in reservationIds)
        {
            await RemoveUnsentAutoRemindersAsync(reservationId, cancellationToken);
        }
    }

    private static string CreateBody(DateTimeOffset occurrenceStart, string marker)
        => $"Your appointment starts at {occurrenceStart:u}.\n\n[{marker}]";

    private static string CreateMarker(string policyId, Guid occurrenceId)
        => $"{MarkerPrefix}:{policyId}:{occurrenceId:N}";

    private static bool IsUnsentAutoReminder(ReminderReadDto reminder)
        => !reminder.IsSent && IsAutoReminder(reminder);

    private static bool IsAutoReminder(ReminderReadDto reminder)
        => reminder.TextBody.Contains(MarkerPrefix, StringComparison.Ordinal);

    private static bool IsAutoReminder(ReminderReadDto reminder, string marker)
        => reminder.TextBody.Contains(marker, StringComparison.Ordinal);

    private sealed record AutoReminderPolicy(string Id, TimeSpan OffsetBeforeStart, ReminderKind Kind);
}
