namespace Vitarish.Appointments.Domain.Dto;

public record BookingPolicyReadDto : EntityReadDto
{
    public Guid SessionId { get; set; }
    public TimeSpan? BookingOpensBefore { get; set; }
    public TimeSpan? BookingCloseBefore { get; set; }
    public bool RequiresApproval { get; set; }
    public TimeSpan? CancellationDeadline { get; set; }
    public bool AllowRescheduling { get; set; }
    public int? MaxReservationsPerParty { get; set; }
}
