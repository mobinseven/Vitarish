namespace Vitarish.Appointments.Domain.Dto;

public record ShiftWriteDto : EntityWriteDto
{
    public ShiftStatus Status { get; set; } = ShiftStatus.Active;
    public ShiftVisibility Visibility { get; set; } = ShiftVisibility.Public;
    public DayOfWeek DayOfWeek { get; set; } = System.DayOfWeek.Monday;
    public TimeOnly StartTime { get; set; } = new(9, 0);
    public TimeOnly EndTime { get; set; } = new(10, 0);
    public string TimeZone { get; set; } = "UTC";
    public int TotalSeats { get; set; } = 10;
    public ShiftBookingPolicyWriteDto? BookingPolicy { get; set; }
}
