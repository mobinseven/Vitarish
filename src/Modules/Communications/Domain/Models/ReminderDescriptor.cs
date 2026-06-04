namespace Vitarish.Communications.Domain.Models;

[EntityDescriptorFor(typeof(Reminder))]
public record ReminderDescriptor : EntityDescriptor
{
    public Guid? SourceId { get; init; }
    public ReminderKind? Kind { get; init; }
    public string? RecipientUserId { get; init; }
    public DateTime? RemindFrom { get; init; }
    public DateTime? RemindTo { get; init; }
    public DateTime? SentFrom { get; init; }
    public DateTime? SentTo { get; init; }
    public bool? IsSent { get; init; }
}
