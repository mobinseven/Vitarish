namespace Vitarish.Appointments.Domain.Entities;

public class Appointment : Entity
{
    public AppointmentStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = AppointmentStatus.Pending;

    public Guid SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }

    public string? UserId
    {
        get;
        set => SetField(ref field, value, nameof(UserId));
    }

    public AppointmentSession? Session { get; set; }
}
