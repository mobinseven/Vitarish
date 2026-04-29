namespace Vitarish.Appointments.Domain.Entities;

public class Appointment : Entity
{
    public Guid? SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }

    public DateTimeOffset ScheduledAt
    {
        get;
        set => SetField(ref field, value, nameof(ScheduledAt));
    }

    public AppointmentStatus Status
    {
        get;
        private set => SetField(ref field, value, nameof(Status));
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

    public void Confirm()
    {
        if (Status is not AppointmentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot confirm an appointment in '{Status}' status.");
        }

        Status = AppointmentStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status is AppointmentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed appointment.");
        }

        Status = AppointmentStatus.Cancelled;
    }

    public void Complete()
    {
        if (Status is not AppointmentStatus.Confirmed)
        {
            throw new InvalidOperationException($"Cannot complete an appointment in '{Status}' status.");
        }

        Status = AppointmentStatus.Completed;
    }
}
