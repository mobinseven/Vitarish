namespace Vitarish.Entities.Server.Core;

public sealed class AuditEntry : Entity
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

    public string? Changes
    {
        get;
        set => SetField(ref field, value, nameof(Changes));
    }

    public DateTimeOffset OccurredAt
    {
        get;
        set => SetField(ref field, value, nameof(OccurredAt));
    }

    public string? CorrelationId
    {
        get;
        set => SetField(ref field, value, nameof(CorrelationId));
    }

    public string? RequestId
    {
        get;
        set => SetField(ref field, value, nameof(RequestId));
    }
}
