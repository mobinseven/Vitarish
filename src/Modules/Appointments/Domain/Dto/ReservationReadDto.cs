namespace Vitarish.Appointments.Domain.Dto;

public record ReservationReadDto : EntityReadDto
{
    public ReservationStatus Status { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public string? ReservingPartyId { get; set; }
    public TimeSpan? EstimatedSessionLength { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public bool IsHold { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
