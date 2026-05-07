namespace Vitarish.Appointments.Domain.Dto;

public record SessionReminderPolicyWriteDto : EntityWriteDto
{
    public Guid SessionId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public SessionReminderKind Kind { get; set; } = SessionReminderKind.Email;
    public TimeSpan OffsetBeforeOccurrenceStart { get; set; } = TimeSpan.FromHours(24);
    public HashSet<ReservationStatus> QualifyingReservationStatuses { get; set; } = [ReservationStatus.Confirmed];
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}
