using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Appointments.Domain.Entities;

public class SessionOccurrence : Entity
{
    public Guid SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }

    public Guid CalendarMappingId
    {
        get;
        set => SetField(ref field, value, nameof(CalendarMappingId));
    }

    public string RecurrenceId
    {
        get;
        set => SetField(ref field, value, nameof(RecurrenceId));
    } = string.Empty;

    public DateTimeOffset OccurrenceStart
    {
        get;
        set => SetField(ref field, value, nameof(OccurrenceStart));
    }

    public DateTimeOffset OccurrenceEnd
    {
        get;
        set => SetField(ref field, value, nameof(OccurrenceEnd));
    }

    public bool IsCancelled
    {
        get;
        set => SetField(ref field, value, nameof(IsCancelled));
    }

    public int? CapacityOverride
    {
        get;
        set => SetField(ref field, value, nameof(CapacityOverride));
    }

    public Session? Session { get; set; }

    [NotMapped]
    public SessionOccurrenceStats Stats { get; set; } = new();
}
