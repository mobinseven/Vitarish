using Vitarish.Appointments.Application.Events;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class SessionReminderPolicyTriggeredHandler(
    ISessionReminderPolicyRepository policyRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    IRemindersService remindersService,
    IMediator mediator,
    TimeProvider? timeProvider = null)
    : INotificationHandler<SessionReminderPolicyTriggeredEvent>
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task Handle(SessionReminderPolicyTriggeredEvent notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notification.ReservingPartyId))
        {
            return;
        }

        IReadOnlyList<SessionReminderPolicy> policies = await policyRepository.GetEnabledBySessionIdAsync(
            await ResolveSessionIdAsync(notification.SessionOccurrenceId, cancellationToken),
            cancellationToken);

        if (policies.Count == 0)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (SessionReminderPolicy policy in policies)
        {
            if (!policy.QualifyingStatuses.Contains(notification.NewStatus))
            {
                continue;
            }

            DateTimeOffset remindAt = notification.OccurrenceStart - policy.OffsetBefore;
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

            string subject = RenderTemplate(policy.SubjectTemplate, notification);
            string body = RenderTemplate(policy.BodyTemplate, notification);

            _ = await remindersService.AddRangeAsync(
                notification.SessionOccurrenceId,
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

    private async Task<Guid> ResolveSessionIdAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = await occurrenceRepository.GetAsync(occurrenceId, cancellationToken);
        return occurrence?.SessionId ?? Guid.Empty;
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

    private static string RenderTemplate(string template, SessionReminderPolicyTriggeredEvent notification)
        => template
            .Replace("{{OccurrenceStart}}", notification.OccurrenceStart.ToString("f"), StringComparison.Ordinal);
}
