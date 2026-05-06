namespace Vitarish.Appointments.Domain.Dto;

public record SessionWriteDto : EntityWriteDto
{
    public string Title { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public SessionVisibility Visibility { get; set; } = SessionVisibility.Public;
    public Guid? CalendarMappingId { get; set; }
    public DateTimeOffset? StartAt { get; set; } = DateTime.Now;
    public DateTimeOffset? EndAt { get; set; } = DateTime.Now.AddHours(1);
    public string TimeZone { get; set; } = "UTC";
    public int TotalSeats { get; set; } = 10;
    public bool RequiresApproval { get; set; }
    public bool AllowRescheduling { get; set; }
}
