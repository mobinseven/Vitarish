namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Appointment))]
public record AppointmentDescriptor : EntityDescriptor
{
    public AppointmentStatus? Status { get; init; }
    public Guid? SessionId { get; init; }
    public string? UserId { get; init; }
}
