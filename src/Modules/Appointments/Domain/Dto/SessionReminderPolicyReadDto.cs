namespace Vitarish.Appointments.Domain.Dto;

public record SessionReminderPolicyReadDto : EntityReadDto
{
    public Guid SessionId { get; set; }
    public bool IsEnabled { get; set; }
    public SessionReminderKind Kind { get; set; }
    public TimeSpan OffsetBeforeOccurrenceStart { get; set; }
    public HashSet<ReservationStatus> QualifyingReservationStatuses { get; set; } = [ReservationStatus.Confirmed];
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}
