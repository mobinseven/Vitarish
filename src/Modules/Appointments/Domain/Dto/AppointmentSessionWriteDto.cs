namespace Vitarish.Appointments.Domain.Dto;

public record AppointmentSessionWriteDto : EntityWriteDto
{
    public DateTimeOffset DateTime { get; set; }

    public int Capacity { get; set; }

    public AppointmentSessionStatus Status { get; set; }
}
