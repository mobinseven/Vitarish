using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Appointments.Domain.Entities;

public class SessionOccurrence : Entity
{
    public Guid SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }

    public DateOnly Date
    {
        get;
        set => SetField(ref field, value, nameof(Date));
    }

    public TimeOnly StartTime
    {
        get;
        set => SetField(ref field, value, nameof(StartTime));
    }

    public TimeOnly EndTime
    {
        get;
        set => SetField(ref field, value, nameof(EndTime));
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

    public bool IsCancelled
    {
        get;
        set => SetField(ref field, value, nameof(IsCancelled));
    }

    public Session? Session { get; set; }

    [NotMapped]
    public SessionOccurrenceStats Stats { get; set; } = new();
}
