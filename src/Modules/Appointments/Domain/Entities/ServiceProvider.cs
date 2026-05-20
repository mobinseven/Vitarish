namespace Vitarish.Appointments.Domain.Entities;

public class ServiceProvider : Entity
{
    public string Name
    {
        get;
        set => SetField(ref field, value, nameof(Name));
    } = string.Empty;

    public string? Description
    {
        get;
        set => SetField(ref field, value, nameof(Description));
    }

    public IList<ServiceProviderUserLink> UserLinks { get; set; } = [];
}
