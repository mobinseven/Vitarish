using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Appointments.Domain.Entities;

public class AppointmentSession : Entity
{
    public DateTimeOffset DateTime
    {
        get;
        set => SetField(ref field, value, nameof(DateTime));
    }

    public int Capacity
    {
        get;
        set => SetField(ref field, value, nameof(Capacity));
    }

    public AppointmentSessionStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = AppointmentSessionStatus.Open;

    [NotMapped]
    public AppointmentSessionStats Stats { get; set; } = new();

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public bool IsFull() => Stats.ReservationCount >= Capacity;
}
