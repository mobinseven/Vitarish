namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(ShiftBookingPolicy))]
public record ShiftBookingPolicyDescriptor : EntityDescriptor
{
    public Guid? ShiftId { get; init; }
    public DateOnly? Date { get; init; }
    public bool? Closed { get; init; }
}
