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

    public string Name
    {
        get;
        set => SetField(ref field, value, nameof(Name));
    } = string.Empty;

    public string Description
    {
        get;
        set => SetField(ref field, value, nameof(Description));
    } = string.Empty;

    public string? AvatarUrl
    {
        get;
        set => SetField(ref field, value, nameof(AvatarUrl));
    }

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
