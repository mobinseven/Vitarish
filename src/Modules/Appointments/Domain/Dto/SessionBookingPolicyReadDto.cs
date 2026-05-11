using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Dto;

public record SessionBookingPolicyReadDto
{
    public Guid Id { get; set; }
    public TimeSpan? BookingOpensBefore { get; set; }
    public TimeSpan? BookingCloseBefore { get; set; }
    public bool RequiresApproval { get; set; }
    public TimeSpan? CancellationDeadline { get; set; }
    public bool AllowRescheduling { get; set; }
    public int? MaxReservationsPerParty { get; set; }
    public IList<ClosedDate> ClosedDates { get; set; } = [];
}
