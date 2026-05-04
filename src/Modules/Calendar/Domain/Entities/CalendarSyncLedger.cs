namespace Vitarish.Calendar.Domain.Entities;

public class CalendarSyncLedger : Entity
{
    public Guid CalendarAccountId
    {
        get;
        set => SetField(ref field, value, nameof(CalendarAccountId));
    }

    public SyncDirection SyncDirection
    {
        get;
        set => SetField(ref field, value, nameof(SyncDirection));
    }

    public DateTimeOffset StartedAt
    {
        get;
        set => SetField(ref field, value, nameof(StartedAt));
    }

    public DateTimeOffset? CompletedAt
    {
        get;
        set => SetField(ref field, value, nameof(CompletedAt));
    }

    public int ItemsProcessed
    {
        get;
        set => SetField(ref field, value, nameof(ItemsProcessed));
    }

    public int Conflicts
    {
        get;
        set => SetField(ref field, value, nameof(Conflicts));
    }

    public int Failures
    {
        get;
        set => SetField(ref field, value, nameof(Failures));
    }

    public string? LastError
    {
        get;
        set => SetField(ref field, value, nameof(LastError));
    }

    public SyncStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = SyncStatus.Pending;
}
