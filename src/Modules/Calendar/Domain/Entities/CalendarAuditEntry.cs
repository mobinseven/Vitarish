namespace Vitarish.Calendar.Domain.Entities;

public class CalendarAuditEntry : Entity
{
    public Guid ObjectMappingId
    {
        get;
        set => SetField(ref field, value, nameof(ObjectMappingId));
    }

    public Guid AccountId
    {
        get;
        set => SetField(ref field, value, nameof(AccountId));
    }

    public CalendarAuditAction Action
    {
        get;
        set => SetField(ref field, value, nameof(Action));
    }

    public string PerformedBy
    {
        get;
        set => SetField(ref field, value, nameof(PerformedBy));
    } = string.Empty;

    public DateTimeOffset PerformedAt
    {
        get;
        set => SetField(ref field, value, nameof(PerformedAt));
    }

    public string? OldValue
    {
        get;
        set => SetField(ref field, value, nameof(OldValue));
    }

    public string? NewValue
    {
        get;
        set => SetField(ref field, value, nameof(NewValue));
    }

    public string? Notes
    {
        get;
        set => SetField(ref field, value, nameof(Notes));
    }
}
