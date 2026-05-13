namespace Vitarish.Appointments.Domain.Dto;

public record SessionReadDto : EntityReadDto
{
    public SessionStatus Status { get; set; }
    public SessionVisibility Visibility { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public int TotalSeats { get; set; } = 1;
    public SessionStats Stats { get; set; } = new();
    public SessionBookingPolicyReadDto? BookingPolicy { get; set; }
    public IReadOnlyList<SessionReminderPolicyReadDto> ReminderPolicies { get; set; } = [];
}
