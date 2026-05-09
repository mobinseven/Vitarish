using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Appointments.Domain.Entities;

public class Session : Entity
{
    public string Title
    {
        get;
        set => SetField(ref field, value, nameof(Title));
    } = string.Empty;

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

    public Guid? CalendarMappingId
    {
        get;
        set => SetField(ref field, value, nameof(CalendarMappingId));
    }

    public DateTimeOffset? StartAt
    {
        get;
        set => SetField(ref field, value, nameof(StartAt));
    }

    public DateTimeOffset? EndAt
    {
        get;
        set => SetField(ref field, value, nameof(EndAt));
    }

    public string TimeZone
    {
        get;
        set => SetField(ref field, value, nameof(TimeZone));
    } = "UTC";

    [NotMapped]
    public DayOfWeek? DayOfWeek => StartAt?.DayOfWeek;

    public int TotalSeats
    {
        get;
        set => SetField(ref field, value, nameof(TotalSeats));
    } = 1;

    public bool RequiresApproval
    {
        get;
        set => SetField(ref field, value, nameof(RequiresApproval));
    }

    public bool AllowRescheduling
    {
        get;
        set => SetField(ref field, value, nameof(AllowRescheduling));
    }

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    [NotMapped]
    public SessionStats Stats { get; set; } = new();
}
