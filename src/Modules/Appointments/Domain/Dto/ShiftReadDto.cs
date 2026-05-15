namespace Vitarish.Appointments.Domain.Dto;

public record ShiftReadDto : EntityReadDto
{
    public ShiftStatus Status { get; set; }
    public ShiftVisibility Visibility { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public int TotalSeats { get; set; } = 1;
    public ShiftStats Stats { get; set; } = new();
    public ShiftBookingPolicyReadDto? BookingPolicy { get; set; }
    public IReadOnlyList<ShiftReminderPolicyReadDto> ReminderPolicies { get; set; } = [];
    public string Title => $"{StartTime:t} - {EndTime:t}";
}
