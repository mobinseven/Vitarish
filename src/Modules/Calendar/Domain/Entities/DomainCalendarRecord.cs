namespace Vitarish.Calendar.Domain.Entities;

public class DomainCalendarRecord : Entity
{
    public Guid MappingId
    {
        get;
        set => SetField(ref field, value, nameof(MappingId));
    }

    public CalendarObjectCategory Category
    {
        get;
        set => SetField(ref field, value, nameof(Category));
    }

    public string BusinessStatus
    {
        get;
        set => SetField(ref field, value, nameof(BusinessStatus));
    } = string.Empty;

    public CalendarObjectVisibility Visibility
    {
        get;
        set => SetField(ref field, value, nameof(Visibility));
    } = CalendarObjectVisibility.Private;

    public string? OwnerId
    {
        get;
        set => SetField(ref field, value, nameof(OwnerId));
    }

    public string? ProviderId
    {
        get;
        set => SetField(ref field, value, nameof(ProviderId));
    }

    public string? ResourceId
    {
        get;
        set => SetField(ref field, value, nameof(ResourceId));
    }

    public string? PatientId
    {
        get;
        set => SetField(ref field, value, nameof(PatientId));
    }

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];
}
