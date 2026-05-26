namespace Vitarish.Appointments.Domain.Models;

public class OpenBookingShiftDate
{
    public string TenantId { get; set; } = default!;
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
}
