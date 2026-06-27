using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Domain.Dto;

public record ReservationReadDto : EntityReadDto
{
    public ReservationStatus Status { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public string? ReservingPartyId { get; set; }
    public TimeSpan? EstimatedSessionLength { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool IsHold { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
