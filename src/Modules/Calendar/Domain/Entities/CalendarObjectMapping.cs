namespace Vitarish.Calendar.Domain.Entities;

public class CalendarObjectMapping : Entity
{
    public Guid CalendarAccountId
    {
        get;
        set => SetField(ref field, value, nameof(CalendarAccountId));
    }

    public string LocalObjectId
    {
        get;
        set => SetField(ref field, value, nameof(LocalObjectId));
    } = string.Empty;

    public string LocalObjectType
    {
        get;
        set => SetField(ref field, value, nameof(LocalObjectType));
    } = string.Empty;

    public string RadicaleResourceUrl
    {
        get;
        set => SetField(ref field, value, nameof(RadicaleResourceUrl));
    } = string.Empty;

    public string ICalendarUid
    {
        get;
        set => SetField(ref field, value, nameof(ICalendarUid));
    } = string.Empty;

    public string? ETag
    {
        get;
        set => SetField(ref field, value, nameof(ETag));
    }

    public CalendarComponentType ComponentType
    {
        get;
        set => SetField(ref field, value, nameof(ComponentType));
    } = CalendarComponentType.VEvent;

    public CalendarObjectCategory Category
    {
        get;
        set => SetField(ref field, value, nameof(Category));
    }

    public DateTimeOffset? LastSyncAt
    {
        get;
        set => SetField(ref field, value, nameof(LastSyncAt));
    }

    public bool IsExternallyManaged
    {
        get;
        set => SetField(ref field, value, nameof(IsExternallyManaged));
    }
}
