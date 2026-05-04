using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Appointments.Domain.Entities;

public class Session : Entity
{
    public string Title
    {
        get;
        set => SetField(ref field, value, nameof(Title));
    } = string.Empty;

    public string? OwnerId
    {
        get;
        set => SetField(ref field, value, nameof(OwnerId));
    }

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

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    [NotMapped]
    public SessionStats Stats { get; set; } = new();
}
