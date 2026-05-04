namespace Vitarish.Calendar.Domain.Entities;

public class CalendarAccount : Entity
{
    public string LocalEntityId
    {
        get;
        set => SetField(ref field, value, nameof(LocalEntityId));
    } = string.Empty;

    public CalendarEntityType LocalEntityType
    {
        get;
        set => SetField(ref field, value, nameof(LocalEntityType));
    }

    public string RadicaleCollectionUrl
    {
        get;
        set => SetField(ref field, value, nameof(RadicaleCollectionUrl));
    } = string.Empty;

    public string DefaultTimeZone
    {
        get;
        set => SetField(ref field, value, nameof(DefaultTimeZone));
    } = "UTC";

    public bool IsActive
    {
        get;
        set => SetField(ref field, value, nameof(IsActive));
    } = true;

    public DateTimeOffset? LastSyncAt
    {
        get;
        set => SetField(ref field, value, nameof(LastSyncAt));
    }

    public SyncStatus SyncStatus
    {
        get;
        set => SetField(ref field, value, nameof(SyncStatus));
    } = SyncStatus.Pending;
}
