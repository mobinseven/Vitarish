namespace Vitarish.Appointments.Domain.Dto;

public record SessionReadDto : EntityReadDto
{
    public DateTimeOffset ScheduledAt { get; set; }

    public int Capacity { get; set; }

    public SessionStatus Status { get; set; }

    public int ReservationCount { get; set; }
}
