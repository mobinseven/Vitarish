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
    public int? BookingOpensBefore { get; set; }
    public int? BookingCloseBefore { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? ServiceId { get; set; }
    [JsonIgnore]
    public string Title => $"{DayOfWeek} {StartTime:t}-{EndTime:t}";
}
