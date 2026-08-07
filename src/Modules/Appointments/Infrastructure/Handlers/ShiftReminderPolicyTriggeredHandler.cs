using Vitarish.Appointments.Application.Events;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class ShiftReminderPolicyTriggeredHandler(
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IEntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor> reminderPolicyRepository,
    IRemindersService remindersService,
    IMediator mediator,
    INotificationTemplateResolver templateResolver,
    IUsersService usersService,
    TimeProvider? timeProvider = null)
    : INotificationHandler<ShiftReminderPolicyTriggeredEvent>
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task Handle(ShiftReminderPolicyTriggeredEvent notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notification.ReservingPartyId))
        {
            return;
        }

        Shift? shift = await shiftRepository.GetAsync(notification.ShiftId, cancellationToken);
        if (shift is null)
        {
            return;
        }

        IReadOnlyList<ShiftReminderPolicy> policies = await reminderPolicyRepository.GetAsync(
            new ShiftReminderPolicyDescriptor { ShiftId = notification.ShiftId, IsEnabled = true },
            cancellationToken);

        if (policies.Count == 0)
        {
            return;
        }

        DateTime occurrenceStart = GetOccurrenceStart(shift, notification.Date);
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        // Resolve recipient locale once for all policies
        string? recipientLocale = await GetUserLocaleAsync(notification.ReservingPartyId, cancellationToken);

        foreach (ShiftReminderPolicy policy in policies)
        {
            if (!policy.QualifyingStatuses.Contains(notification.NewStatus))
            {
                continue;
            }

            DateTime remindAt = occurrenceStart - policy.OffsetBefore;
            if (remindAt <= now)
            {
                continue;
            }

            string? recipient = await ResolveRecipientAsync(
                notification.ReservingPartyId,
                policy.Channel,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(recipient))
            {
                continue;
            }

            if (policy.Channel == ShiftReminderChannel.Email)
            {
                // Pre-render email so Sea ReminderProcessor sends directly without Sea entity lookups.
                ResolvedNotificationTemplate? template = await templateResolver.ResolveAsync(
                    recipientLocale,
                    NotificationPurpose.AppointmentReminder,
                    cancellationToken);

                string subject = RenderTemplate(template?.EmailSubject ?? string.Empty, occurrenceStart);
                string body = RenderTemplate(template?.EmailTextBody ?? string.Empty, occurrenceStart);

                _ = await remindersService.AddRangeAsync(
                    notification.ShiftId,
                    [new ReminderRecipientReadDto
                    {
                        Kind = Vitarish.Communications.Domain.Enums.ReminderKind.Email,
                        RecipientUserId = notification.ReservingPartyId,
                        Recipient = recipient
                    }],
                    subject,
                    body,
                    remindAt,
                    "appointment",
                    NotificationPurpose.AppointmentReminder.ToString(),
                    null,
                    cancellationToken);
            }
            else
            {
                // For SMS, store empty body so Sea ReminderProcessor resolves the SMS template
                // from Platform locale records and sends via SendTemplatedSms.
                _ = await remindersService.AddRangeAsync(
                    notification.ShiftId,
                    [new ReminderRecipientReadDto
                    {
                        Kind = Vitarish.Communications.Domain.Enums.ReminderKind.Sms,
                        RecipientUserId = notification.ReservingPartyId,
                        Recipient = recipient
                    }],
                    string.Empty,
                    string.Empty,
                    remindAt,
                    "appointment",
                    NotificationPurpose.AppointmentReminder.ToString(),
                    null,
                    cancellationToken);
            }
        }
    }

    private static DateTime GetOccurrenceStart(Shift shift, DateOnly date)
    {
        DateTime localOccurrenceStart = DateTime.SpecifyKind(date.ToDateTime(shift.StartTime), DateTimeKind.Unspecified);

        if (string.IsNullOrWhiteSpace(shift.TimeZone))
        {
            return DateTime.SpecifyKind(localOccurrenceStart, DateTimeKind.Utc);
        }

        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(shift.TimeZone);
            return TimeZoneInfo.ConvertTimeToUtc(localOccurrenceStart, timeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return DateTime.SpecifyKind(localOccurrenceStart, DateTimeKind.Utc);
        }
    }

    private async Task<string?> ResolveRecipientAsync(
        string userId,
        ShiftReminderChannel channel,
        CancellationToken cancellationToken)
    {
        Vitarish.Users.Domain.Entities.User? user = await mediator.Send(new GetUserQuery(userId), cancellationToken);
        return user is null
            ? null
            : channel switch
            {
                ShiftReminderChannel.Email => user.Email,
                ShiftReminderChannel.Sms => user.PhoneNumber,
                _ => null
            };
    }

    private async Task<string?> GetUserLocaleAsync(string userId, CancellationToken cancellationToken)
    {
        Vitarish.Users.Domain.Entities.User? user = await usersService.GetUserAsync(userId, cancellationToken);
        if (user?.ProfilePreferences is null) return null;
        return user.ProfilePreferences.TryGetValue("Culture", out string? culture) ? culture : null;
    }

    private static string RenderTemplate(string template, DateTime occurrenceStart)
        => template.Replace("{OccurrenceStart}", occurrenceStart.ToString("f"), StringComparison.Ordinal);
}

