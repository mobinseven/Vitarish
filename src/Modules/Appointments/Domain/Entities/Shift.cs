namespace Vitarish.Appointments.Domain.Entities;

public class Shift : Entity
{
    public ShiftStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = ShiftStatus.Draft;

    public ShiftVisibility Visibility
    {
        get;
        set => SetField(ref field, value, nameof(Visibility));
    } = ShiftVisibility.Public;

    public DayOfWeek DayOfWeek
    {
        get;
        set => SetField(ref field, value, nameof(DayOfWeek));
    }

    public TimeOnly StartTime
    {
        get;
        set => SetField(ref field, value, nameof(StartTime));
    } = new(9, 0);

    public TimeOnly EndTime
    {
        get;
        set => SetField(ref field, value, nameof(EndTime));
    } = new(10, 0);

    public string TimeZone
    {
        get;
        set => SetField(ref field, value, nameof(TimeZone));
    } = "UTC";

    public int TotalSeats
    {
        get;
        set => SetField(ref field, value, nameof(TotalSeats));
    } = 1;

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public Guid? ServiceProviderId
    {
        get;
        set => SetField(ref field, value, nameof(ServiceProviderId));
    }

    public Guid? ServiceId
    {
        get;
        set => SetField(ref field, value, nameof(ServiceId));
    }

    public ShiftBookingPolicy? BookingPolicy { get; set; }

    public IList<ShiftReminderPolicy> ReminderPolicies { get; set; } = [];

}
