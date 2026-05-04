namespace Vitarish.Appointments.Domain.Entities;

public class BookingPolicy : Entity
{
    public Guid SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }

    public TimeSpan? BookingOpensBefore
    {
        get;
        set => SetField(ref field, value, nameof(BookingOpensBefore));
    }

    public TimeSpan? BookingCloseBefore
    {
        get;
        set => SetField(ref field, value, nameof(BookingCloseBefore));
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
