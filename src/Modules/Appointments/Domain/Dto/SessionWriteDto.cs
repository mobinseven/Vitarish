namespace Vitarish.Appointments.Domain.Dto;

public record SessionWriteDto : EntityWriteDto
{
    public string Title { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public SessionVisibility Visibility { get; set; } = SessionVisibility.Public;
    public DayOfWeek DayOfWeek { get; set; } = System.DayOfWeek.Monday;
    public TimeOnly StartTime { get; set; } = new(9, 0);
    public TimeOnly EndTime { get; set; } = new(10, 0);
    public string TimeZone { get; set; } = "UTC";
    public int TotalSeats { get; set; } = 10;
    public bool RequiresApproval { get; set; }
    public bool AllowRescheduling { get; set; }
}
