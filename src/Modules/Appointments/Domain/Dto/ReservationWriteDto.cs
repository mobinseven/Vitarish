namespace Vitarish.Appointments.Domain.Dto;

public record ReservationWriteDto : EntityWriteDto
{
    public ReservationStatus Status { get; set; }
    public Guid SessionOccurrenceId { get; set; }
    public string? ReservingPartyId { get; set; }
}
