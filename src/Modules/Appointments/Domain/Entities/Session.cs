namespace Vitarish.Appointments.Domain.Entities;

public class Session : Entity
{
    public DateTimeOffset ScheduledAt
    {
        get;
        set => SetField(ref field, value, nameof(ScheduledAt));
    }

    public int Capacity
    {
        get;
        set => SetField(ref field, value, nameof(Capacity));
    }

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public SessionStatus Status
    {
        get;
        private set => SetField(ref field, value, nameof(Status));
    } = SessionStatus.Active;

    public bool IsFull(int currentReservations) => currentReservations >= Capacity;

    public void Cancel()
    {
        if (Status is SessionStatus.Cancelled)
        {
            throw new InvalidOperationException("Session is already cancelled.");
        }

        Status = SessionStatus.Cancelled;
    }
}
