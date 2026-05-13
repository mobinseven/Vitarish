using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Appointments.Domain.Entities;

public class Session : Entity
{
    public SessionStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = SessionStatus.Draft;

    public SessionVisibility Visibility
    {
        get;
        set => SetField(ref field, value, nameof(Visibility));
    } = SessionVisibility.Public;

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

    public SessionBookingPolicy? BookingPolicy { get; set; }

    public IList<SessionReminderPolicy> ReminderPolicies { get; set; } = [];

    [NotMapped]
    public SessionStats Stats { get; set; } = new();
}
