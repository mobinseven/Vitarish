namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(SessionReminderPolicy))]
public record SessionReminderPolicyDescriptor : EntityDescriptor
{
    public Guid? SessionId { get; init; }
    public bool? IsEnabled { get; init; }
    public SessionReminderKind? Kind { get; init; }
}
