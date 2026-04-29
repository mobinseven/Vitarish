namespace Vitarish.Appointments.Domain.Dto;

public record AppointmentWriteDto : EntityWriteDto
{
    public DateTimeOffset ScheduledAt { get; set; }

    public AppointmentStatus Status { get; set; }

    public string? Notes { get; set; }

    public Guid? SessionId { get; set; }
}
