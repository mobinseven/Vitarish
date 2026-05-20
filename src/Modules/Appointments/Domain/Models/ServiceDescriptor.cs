namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Service))]
public record ServiceDescriptor : EntityDescriptor
{
    public string? Name { get; init; }
    public Guid? ServiceTypeId { get; init; }
}
