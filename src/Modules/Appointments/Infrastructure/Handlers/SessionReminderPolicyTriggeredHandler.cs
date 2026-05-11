using Vitarish.Appointments.Application.Events;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class SessionReminderPolicyTriggeredHandler(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
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

        Session? session = await sessionRepository.GetAsync(notification.SessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        IEnumerable<SessionReminderPolicy> enabledPolicies = session.ReminderPolicies.Where(p => p.IsEnabled);
        if (!enabledPolicies.Any())
        {
            return;
        }

        DateTimeOffset occurrenceStart = new(notification.Date.ToDateTime(session.StartTime), TimeSpan.Zero);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (SessionReminderPolicy policy in enabledPolicies)
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

            string subject = RenderTemplate(policy.SubjectTemplate, session.Title, occurrenceStart);
            string body = RenderTemplate(policy.BodyTemplate, session.Title, occurrenceStart);

            _ = await remindersService.AddRangeAsync(
                notification.SessionId,
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

    private static string RenderTemplate(string template, string sessionTitle, DateTimeOffset occurrenceStart)
        => template
            .Replace("{{SessionTitle}}", sessionTitle, StringComparison.Ordinal)
            .Replace("{{OccurrenceStart}}", occurrenceStart.ToString("f"), StringComparison.Ordinal);
}

