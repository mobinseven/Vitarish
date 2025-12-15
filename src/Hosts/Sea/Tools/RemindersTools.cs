using System.ComponentModel;


namespace Vitarish.Sea.Tools;

[McpServerToolType]
public static class RemindersTools
{
    [McpServerTool(Name = nameof(AddRemindersAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command adds reminders for an event.")]
    public static Task<IReadOnlyList<ReminderDto>> AddRemindersAsync(
            IRemindersClient RemindersService,
            Guid eventId,
            IReadOnlyList<ReminderRecipientDto> recipients,
            string subject,
            string textBody,
            DateTimeOffset remindAt,
            CancellationToken cancellationToken = default) => RemindersService.AddRangeAsync(eventId, recipients, subject, textBody, remindAt, cancellationToken);

    [McpServerTool(Name = nameof(UpdateReminderAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command updates an existing reminder.")]
    public static Task<ReminderDto?> UpdateReminderAsync(
            IRemindersClient RemindersService,
            Guid id,
            ReminderKind kind,
            string recipientUserId,
            string recipient,
            string subject,
            string textBody,
            DateTimeOffset remindAt,
            CancellationToken cancellationToken = default) => RemindersService.UpdateAsync(id, kind, recipientUserId, recipient, subject, textBody, remindAt, cancellationToken);

    [McpServerTool(Name = nameof(RemoveReminderAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command removes a reminder.")]
    public static Task RemoveReminderAsync(
            IRemindersClient RemindersService,
            Guid id,
            CancellationToken cancellationToken = default) => RemindersService.RemoveByIdAsync(id, cancellationToken);

    [McpServerTool(Name = nameof(GetRemindersByEventAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves reminders for an event.")]
    public static Task<IReadOnlyList<ReminderDto>> GetRemindersByEventAsync(
            IRemindersClient RemindersService,
            Guid eventId,
            CancellationToken cancellationToken = default) => RemindersService.GetByEventAsync(eventId, cancellationToken);

    [McpServerTool(Name = nameof(GetDueRemindersAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves reminders that are due by a specified time.")]
    public static Task<IReadOnlyList<ReminderDto>> GetDueRemindersAsync(
            IRemindersClient RemindersService,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) => RemindersService.GetDueAsync(now, cancellationToken);

    [McpServerTool(Name = nameof(MarkReminderSentAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command marks a reminder as sent.")]
    public static Task MarkReminderSentAsync(
            IRemindersClient RemindersService,
            Guid id,
            DateTimeOffset sentAt,
            CancellationToken cancellationToken = default) => RemindersService.MarkSentAsync(id, sentAt, cancellationToken);
}

