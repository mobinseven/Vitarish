namespace Vitarish.Appointments.Domain.Dto;

public record SessionReadDto : EntityReadDto
{
    public string Title { get; set; } = string.Empty;
    public string? OwnerId { get; set; }
    public SessionStatus Status { get; set; }
    public SessionVisibility Visibility { get; set; }
    public Guid? CalendarMappingId { get; set; }
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public int TotalSeats { get; set; } = 1;
    public bool RequiresApproval { get; set; }
    public bool AllowRescheduling { get; set; }
    public SessionStats Stats { get; set; } = new();
}
