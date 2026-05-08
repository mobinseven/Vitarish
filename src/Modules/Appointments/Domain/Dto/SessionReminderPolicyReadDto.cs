namespace Vitarish.Appointments.Domain.Dto;

public record SessionReminderPolicyReadDto : EntityReadDto
{
    public Guid SessionId { get; set; }
    public bool IsEnabled { get; set; }
    public ReminderKind Channel { get; set; }
    public TimeSpan OffsetBefore { get; set; }
    public IList<ReservationStatus> QualifyingStatuses { get; set; } = [];
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string Url => $"/sessions/{SessionId}/reminder-policies/{Id}";
}
