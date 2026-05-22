namespace Vitarish.Appointments.Domain.Dto;

public record ReservationWriteDto : EntityWriteDto
{
    public ReservationStatus Status { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public string? ReservingPartyId { get; set; }
    public TimeSpan? EstimatedSessionLength { get; set; }
}
