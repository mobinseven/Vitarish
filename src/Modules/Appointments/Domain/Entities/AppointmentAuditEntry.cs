namespace Vitarish.Appointments.Domain.Entities;

public class AppointmentAuditEntry : Entity
{
    public string Action
    {
        get;
        set => SetField(ref field, value, nameof(Action));
    } = string.Empty;

    public string? ActorId
    {
        get;
        set => SetField(ref field, value, nameof(ActorId));
    }

    public string EntityType
    {
        get;
        set => SetField(ref field, value, nameof(EntityType));
    } = string.Empty;

    public Guid EntityId
    {
        get;
        set => SetField(ref field, value, nameof(EntityId));
    }

    public string? SnapshotBefore
    {
        get;
        set => SetField(ref field, value, nameof(SnapshotBefore));
    }

    public string? SnapshotAfter
    {
        get;
        set => SetField(ref field, value, nameof(SnapshotAfter));
    }

    public DateTimeOffset OccurredAt
    {
        get;
        set => SetField(ref field, value, nameof(OccurredAt));
    }
}
