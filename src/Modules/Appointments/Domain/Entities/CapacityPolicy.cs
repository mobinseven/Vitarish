namespace Vitarish.Appointments.Domain.Entities;

public class CapacityPolicy : Entity
{
    public Guid SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }

    public int TotalSeats
    {
        get;
        set => SetField(ref field, value, nameof(TotalSeats));
    } = 1;

    public bool PendingConsumesCapacity
    {
        get;
        set => SetField(ref field, value, nameof(PendingConsumesCapacity));
    } = true;

    public bool WaitlistAllowed
    {
        get;
        set => SetField(ref field, value, nameof(WaitlistAllowed));
    }

    public int? MaxWaitlistSize
    {
        get;
        set => SetField(ref field, value, nameof(MaxWaitlistSize));
    }
}
