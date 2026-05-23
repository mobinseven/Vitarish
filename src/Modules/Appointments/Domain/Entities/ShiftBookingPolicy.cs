namespace Vitarish.Appointments.Domain.Entities;

public class ShiftBookingPolicy : Entity
{
    public Guid ShiftId
    {
        get;
        set => SetField(ref field, value, nameof(ShiftId));
    }

    public DateOnly Date
    {
        get;
        set => SetField(ref field, value, nameof(Date));
    }

    public int? BookingOpensBefore
    {
        get;
        set => SetField(ref field, value, nameof(BookingOpensBefore));
    }

    public int? BookingCloseBefore
    {
        get;
        set => SetField(ref field, value, nameof(BookingCloseBefore));
    }

    public bool Closed
    {
        get;
        set => SetField(ref field, value, nameof(Closed));
    }

    public int? Capacity
    {
        get;
        set => SetField(ref field, value, nameof(Capacity));
    }

    public bool RequiresApproval
    {
        get;
        set => SetField(ref field, value, nameof(RequiresApproval));
    }

    public TimeSpan? CancellationDeadline
    {
        get;
        set => SetField(ref field, value, nameof(CancellationDeadline));
    }

    public TimeSpan? EstimatedSessionLength
    {
        get;
        set => SetField(ref field, value, nameof(EstimatedSessionLength));
    }

    public bool AllowRescheduling
    {
        get;
        set => SetField(ref field, value, nameof(AllowRescheduling));
    }

    public int? MaxReservationsPerParty
    {
        get;
        set => SetField(ref field, value, nameof(MaxReservationsPerParty));
    }
}
