namespace Vitarish.Appointments.Domain.Dto;

public record AppointmentSessionReadDto : EntityReadDto
{
    public DateTimeOffset DateTime { get; set; }

    public int Capacity { get; set; }

    public AppointmentSessionStatus Status { get; set; }

    public AppointmentSessionStats Stats { get; set; } = new();
}
