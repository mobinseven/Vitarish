namespace Vitarish.Appointments.Domain.Dto;

public record ShiftReminderPolicyReadDto : EntityReadDto
{
    public Guid ShiftId { get; set; }
    public bool IsEnabled { get; set; }
    public ReminderKind Channel { get; set; }
    public TimeSpan OffsetBefore { get; set; }
    public IList<ReservationStatus> QualifyingStatuses { get; set; } = [];
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;

    [JsonIgnore]
    public string Url => $"/shifts/{ShiftId}/reminder-policies/{Id}";
}
