namespace Vitarish.Communications.Domain.Entities;

public sealed class ReminderDeliveryAttempt : Entity
{
    public Guid ReminderId { get; set; }
    public Reminder Reminder { get; set; } = default!;
    public ReminderKind Kind { get; set; }
    public DateTime AttemptedAt { get; set; }
    public bool Succeeded { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Failure { get; set; }
}
