using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Entities;

public class ShiftBookingPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public int? BookingOpensBefore { get; set; }
    public int? BookingCloseBefore { get; set; }
    public bool Closed { get; set; }
    public int? Capacity { get; set; }
    public bool RequiresApproval { get; set; }
    public TimeSpan? CancellationDeadline { get; set; }
    public TimeSpan? EstimatedSessionLength { get; set; }
    public bool AllowRescheduling { get; set; }
    public int? MaxReservationsPerParty { get; set; }
}
