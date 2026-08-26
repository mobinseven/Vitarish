namespace Vitarish.Communications.Infrastructure.Services;

using Microsoft.EntityFrameworkCore.Storage;

[RegisterTransient]
internal class RemindersService(IMediator mediator, CommunicationsDbContext db) : EntityService<Reminder, ReminderDescriptor, ReminderReadDto, ReminderWriteDto, ReminderDescriptorDto>
    (
        mediator,
        reminder => reminder.ToRead(),
        reminder => reminder.ToEntity(),
        reminder => reminder.ToDescriptor()
    ),
    IRemindersService
{
    public async Task<IReadOnlyList<ReminderReadDto>> AddRangeAsync(
        Guid sourceId,
        IReadOnlyList<ReminderRecipientReadDto> recipients,
        string subject,
        string textBody,
        DateTime remindAt,
        string sourceKind,
        string templatePurpose,
        string? templateVariablesJson,
        CancellationToken cancellationToken)
    {
        List<Reminder> reminders = [
            .. recipients.Select(r => new Reminder
            {
                SourceId = sourceId,
                SourceKind = sourceKind,
                TemplatePurpose = templatePurpose,
                TemplateVariablesJson = templateVariablesJson,
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
        return [.. added.Select(reminder => reminder.ToRead())];
    }

    public async Task ReplaceBySourceAsync(
        Guid sourceId,
        IReadOnlyList<ReminderWriteDto> reminders,
        CancellationToken cancellationToken)
    {
        DateTime deletedAt = DateTime.UtcNow;
        IExecutionStrategy executionStrategy = db.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            List<Reminder> existing = await db.Reminders
                .Where(reminder => reminder.SourceId == sourceId)
                .ToListAsync(cancellationToken);
            foreach (Reminder reminder in existing.Where(reminder => !reminder.IsSent))
            {
                reminder.DeletedAt = deletedAt;
            }

            await db.Reminders.AddRangeAsync(reminders.Select(reminder => reminder.ToEntity()), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task<ReminderReadDto?> UpdateAsync(
        Guid id,
        ReminderKind kind,
        string? recipientUserId,
        string recipient,
        string subject,
        string textBody,
        DateTime remindAt,
        string? sourceKind,
        string? templatePurpose,
        string? templateVariablesJson,
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
        if (!string.IsNullOrWhiteSpace(sourceKind)) reminder.SourceKind = sourceKind;
        if (!string.IsNullOrWhiteSpace(templatePurpose)) reminder.TemplatePurpose = templatePurpose;
        reminder.TemplateVariablesJson = templateVariablesJson;

        Reminder updated = (await UpdateEntityAsync(reminder, cancellationToken))
            .EnsureValue("Update reminder");
        return updated.ToRead();
    }

    public async Task<IReadOnlyList<ReminderReadDto>> GetBySourceAsync(Guid sourceId, CancellationToken cancellationToken) => (await GetListAsync(
                new ReminderDescriptor { SourceId = sourceId },
                cancellationToken)).EnsureValue("Get reminders by source");

    public async Task<IReadOnlyList<ReminderReadDto>> GetDueAsync(DateTime now, CancellationToken cancellationToken) => (await GetListAsync(
                new ReminderDescriptor { RemindTo = now, IsSent = false },
                cancellationToken)).EnsureValue("Get due reminders");

    public async Task MarkSentAsync(Guid id, DateTime sentAt, CancellationToken cancellationToken) => await Mediator.Send(new MarkReminderSentCommand(id, sentAt), cancellationToken);

    public async Task RecordProviderSubmissionAsync(Guid id, string providerMessageId, DateTime submittedAt, CancellationToken cancellationToken)
    {
        Reminder? reminder = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (reminder is null) throw new InvalidOperationException("Reminder was not found.");
        reminder.RecordProviderSubmission(providerMessageId, submittedAt);
        _ = await UpdateEntityAsync(reminder, cancellationToken);
    }

    public async Task RecordDeliveryAttemptAsync(Guid id, ReminderKind kind, DateTime attemptedAt, bool succeeded, string? providerMessageId, string? failure, CancellationToken cancellationToken)
    {
        Reminder? reminder = await db.Reminders.SingleOrDefaultAsync(reminder => reminder.Id == id, cancellationToken);
        if (reminder is null) throw new InvalidOperationException("Reminder was not found.");
        if (succeeded && !string.IsNullOrWhiteSpace(providerMessageId))
            reminder.RecordProviderSubmission(providerMessageId, attemptedAt);
        else if (succeeded)
            reminder.RecordDeliveryAcceptance(attemptedAt);
        _ = await db.ReminderDeliveryAttempts.AddAsync(new ReminderDeliveryAttempt
        {
            ReminderId = id,
            Kind = kind,
            AttemptedAt = attemptedAt,
            Succeeded = succeeded,
            ProviderMessageId = providerMessageId,
            Failure = failure is null ? null : failure.Length <= 1000 ? failure : failure[..1000]
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
