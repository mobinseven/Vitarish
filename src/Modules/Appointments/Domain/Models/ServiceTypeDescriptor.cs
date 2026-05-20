namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(ServiceType))]
public record ServiceTypeDescriptor : EntityDescriptor
{
    public string? Name { get; init; }
}
