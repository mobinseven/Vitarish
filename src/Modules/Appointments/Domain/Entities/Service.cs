namespace Vitarish.Appointments.Domain.Entities;

public class Service : Entity
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

    public Guid ServiceTypeId
    {
        get;
        set => SetField(ref field, value, nameof(ServiceTypeId));
    }

    public ServiceType? ServiceType { get; set; }
}
