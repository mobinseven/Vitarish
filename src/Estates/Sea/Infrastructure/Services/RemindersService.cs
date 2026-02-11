using Vitarish.Sea.Application.Commands.Reminders;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class RemindersService(IMediator mediator, RemindersMapper mapper) : EntityService<Reminder, ReminderDescriptor, ReminderReadDto, ReminderWriteDto, ReminderDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IRemindersService
{

    public async Task<IReadOnlyList<ReminderReadDto>> AddRangeAsync(
        Guid eventId,
        IReadOnlyList<ReminderRecipientDto> recipients,
        string subject,
        string textBody,
        DateTimeOffset remindAt,
        CancellationToken cancellationToken)
    {
        List<Reminder> reminders = [
            .. recipients.Select(r => new Reminder(
                eventId,
                r.Kind,
                r.RecipientUserId,
                r.Recipient,
                subject,
                textBody,
                remindAt))
        ];

        IReadOnlyList<Reminder> added = (await AddEntitiesAsync(reminders, cancellationToken))
            .EnsureValue("Add reminders");
        return [.. added.Select(mapper.ToDto)];
    }

    public async Task<ReminderReadDto?> UpdateAsync(
        Guid id,
        ReminderKind kind,
        string recipientUserId,
        string recipient,
        string subject,
        string textBody,
        DateTimeOffset remindAt,
        CancellationToken cancellationToken)
    {
        Reminder? reminder = (await GetEntityAsync(
            id,
            cancellationToken)).GetValueOrDefault();

        if (reminder is null)
        {
            return null;
        }

        reminder.SetKind(kind);
        reminder.RecipientUserId = recipientUserId;
        reminder.Recipient = recipient;
        reminder.Subject = subject;
        reminder.TextBody = textBody;
        reminder.RemindAt = remindAt;

        Reminder updated = (await UpdateEntityAsync(reminder, cancellationToken))
            .EnsureValue("Update reminder");
        return mapper.ToDto(updated);
    }

    public async Task<IReadOnlyList<ReminderReadDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) => (await GetListAsync(
                new ReminderDescriptor { EventId = eventId },
                cancellationToken)).EnsureValue("Get reminders by event");

    public async Task<IReadOnlyList<ReminderReadDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken) => (await GetListAsync(
                new ReminderDescriptor { RemindTo = now, IsSent = false },
                cancellationToken)).EnsureValue("Get due reminders");

    public async Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken) => await Mediator.Send(new MarkReminderSentCommand(id, sentAt), cancellationToken);

}

