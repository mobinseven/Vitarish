namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(ServiceProvider))]
public record ServiceProviderDescriptor : EntityDescriptor
{
    public string? Name { get; init; }
}
