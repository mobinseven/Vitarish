namespace Vitarish.Appointments.Domain.Entities;

public class Appointment : Entity
{
    public DateTimeOffset ScheduledAt
    {
        get;
        set => SetField(ref field, value, nameof(ScheduledAt));
    }

    public AppointmentStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = AppointmentStatus.Pending;

    public string Notes
    {
        get;
        set => SetField(ref field, value, nameof(Notes));
    } = string.Empty;

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public Guid? SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }
}
