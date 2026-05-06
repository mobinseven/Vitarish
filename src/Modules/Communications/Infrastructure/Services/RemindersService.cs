namespace Vitarish.Communications.Infrastructure.Services;

[RegisterTransient]
internal class RemindersService(IMediator mediator) : EntityService<Reminder, ReminderDescriptor, ReminderReadDto, ReminderWriteDto, ReminderDescriptorDto>
    (
        mediator,
        reminder => reminder.ToDto(),
        reminder => reminder.ToDomain(),
        reminder => reminder.ToDomain()
    ),
    IRemindersService
{
    public async Task<IReadOnlyList<ReminderReadDto>> AddRangeAsync(
        Guid sourceId,
        IReadOnlyList<ReminderRecipientReadDto> recipients,
        string subject,
        string textBody,
        DateTimeOffset remindAt,
        CancellationToken cancellationToken)
    {
        List<Reminder> reminders = [
            .. recipients.Select(r => new Reminder
            {
                SourceId = sourceId,
                Kind = r.Kind,
                RecipientUserId = r.RecipientUserId,
                Recipient = r.Recipient,
                Subject = subject,
                TextBody = textBody,
                RemindAt = remindAt
            })
        ];

        IReadOnlyList<Reminder> added = (await AddEntitiesAsync(reminders, cancellationToken))
            .EnsureValue("Add reminders");
        return [.. added.Select(reminder => reminder.ToDto())];
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
        return updated.ToDto();
    }

    public async Task<IReadOnlyList<ReminderReadDto>> GetBySourceAsync(Guid sourceId, CancellationToken cancellationToken) => (await GetListAsync(
                new ReminderDescriptor { SourceId = sourceId },
                cancellationToken)).EnsureValue("Get reminders by source");

    public async Task<IReadOnlyList<ReminderReadDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken) => (await GetListAsync(
                new ReminderDescriptor { RemindTo = now, IsSent = false },
                cancellationToken)).EnsureValue("Get due reminders");

    public async Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken) => await Mediator.Send(new MarkReminderSentCommand(id, sentAt), cancellationToken);
}
