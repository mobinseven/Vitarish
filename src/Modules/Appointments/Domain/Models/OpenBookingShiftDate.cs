namespace Vitarish.Appointments.Domain.Models;

public class OpenBookingShiftDate
{
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
}
