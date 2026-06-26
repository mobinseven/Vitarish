namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(ShiftReminderPolicy))]
public record ShiftReminderPolicyDescriptor : EntityDescriptor
{
    public Guid? ShiftId { get; init; }
    public bool? IsEnabled { get; init; }
    public ShiftReminderChannel? Channel { get; init; }
}
