using Vitarish.Appointments.Application.Events;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class ShiftReminderPolicyTriggeredHandler(
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IEntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor> reminderPolicyRepository,
    IRemindersService remindersService,
    IMediator mediator,
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

        DateTimeOffset occurrenceStart = new(notification.Date.ToDateTime(shift.StartTime), TimeSpan.Zero);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (ShiftReminderPolicy policy in policies)
        {
            if (!policy.QualifyingStatuses.Contains(notification.NewStatus))
            {
                continue;
            }

            DateTimeOffset remindAt = occurrenceStart - policy.OffsetBefore;
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

            string subject = RenderTemplate(policy.SubjectTemplate, occurrenceStart);
            string body = RenderTemplate(policy.BodyTemplate, occurrenceStart);

            _ = await remindersService.AddRangeAsync(
                notification.ShiftId,
                [new ReminderRecipientReadDto
                {
                    Kind = policy.Channel,
                    RecipientUserId = notification.ReservingPartyId,
                    Recipient = recipient
                }],
                subject,
                body,
                remindAt,
                cancellationToken);
        }
    }

    private async Task<string?> ResolveRecipientAsync(
        string userId,
        ReminderKind channel,
        CancellationToken cancellationToken)
    {
        Vitarish.Users.Domain.Entities.User? user = await mediator.Send(new GetUserQuery(userId), cancellationToken);
        if (user is null)
        {
            return null;
        }

        return channel switch
        {
            ReminderKind.Email => user.Email,
            ReminderKind.Sms => user.PhoneNumber,
            _ => null
        };
    }

    private static string RenderTemplate(string template, DateTimeOffset occurrenceStart)
        => template.Replace("{{OccurrenceStart}}", occurrenceStart.ToString("f"), StringComparison.Ordinal);
}

