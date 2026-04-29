namespace Vitarish.Appointments.Domain.Dto;

public record SessionWriteDto : EntityWriteDto
{
    public DateTimeOffset ScheduledAt { get; set; }

    public int Capacity { get; set; }
}
