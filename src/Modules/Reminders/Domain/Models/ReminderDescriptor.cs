namespace Vitarish.Reminders.Domain.Models;

[EntityDescriptorFor(typeof(Reminder))]
public record ReminderDescriptor : EntityDescriptor
{
    public Guid? SourceId { get; init; }
    public ReminderKind? Kind { get; init; }
    public string? RecipientUserId { get; init; }
    public DateTimeOffset? RemindFrom { get; init; }
    public DateTimeOffset? RemindTo { get; init; }
    public DateTimeOffset? SentFrom { get; init; }
    public DateTimeOffset? SentTo { get; init; }
    public bool? IsSent { get; init; }
}
