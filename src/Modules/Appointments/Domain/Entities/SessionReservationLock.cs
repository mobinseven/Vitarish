namespace Vitarish.Appointments.Domain.Entities;

public class SessionReservationLock
{
    public Guid SessionId { get; set; }
    public DateOnly Date { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
