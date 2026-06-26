namespace Vitarish.Appointments.Contracts.Dto;

public record ShiftBookingPolicyWriteDto : EntityWriteDto
{
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
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
