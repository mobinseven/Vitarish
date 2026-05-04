using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Calendar.Domain.Entities;

public class OccurrenceProjection : Entity
{
    public Guid MappingId
    {
        get;
        set => SetField(ref field, value, nameof(MappingId));
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

    public bool IsException
    {
        get;
        set => SetField(ref field, value, nameof(IsException));
    }

    public bool IsCancelled
    {
        get;
        set => SetField(ref field, value, nameof(IsCancelled));
    }

    public DateTimeOffset ProjectedAt
    {
        get;
        set => SetField(ref field, value, nameof(ProjectedAt));
    }

    public DateTimeOffset HorizonEnd
    {
        get;
        set => SetField(ref field, value, nameof(HorizonEnd));
    }
}
