using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Entities;

public class SessionBookingPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TimeSpan? BookingOpensBefore { get; set; }
    public TimeSpan? BookingCloseBefore { get; set; }
    public bool RequiresApproval { get; set; }
    public TimeSpan? CancellationDeadline { get; set; }
    public bool AllowRescheduling { get; set; }
    public int? MaxReservationsPerParty { get; set; }
    public IList<ClosedDate> ClosedDates { get; set; } = [];
}
