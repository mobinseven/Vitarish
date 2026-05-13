namespace Vitarish.Appointments.Domain.Entities;

public class ShiftReservationLock
{
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
