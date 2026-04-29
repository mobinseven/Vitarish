namespace Vitarish.Appointments.Domain.Dto;

public record AppointmentReadDto : EntityReadDto
{
    public DateTimeOffset ScheduledAt { get; set; }

    public AppointmentStatus Status { get; set; }

    public string Notes { get; set; } = string.Empty;

    public Guid? SessionId { get; set; }
}
