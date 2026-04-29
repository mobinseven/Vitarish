namespace Vitarish.Appointments.Domain.Dto;

public record SessionWriteDto : EntityWriteDto
{
    public DateTimeOffset ScheduledAt { get; set; }

    public int Capacity { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? AvatarUrl { get; set; }
}
