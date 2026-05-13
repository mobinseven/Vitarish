namespace Vitarish.Appointments.Domain.Models;

public class ShiftReservation
{
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int TotalSeats { get; set; }
    public int ReservationCount { get; set; }
    public int AvailableSeats { get; set; }
}
