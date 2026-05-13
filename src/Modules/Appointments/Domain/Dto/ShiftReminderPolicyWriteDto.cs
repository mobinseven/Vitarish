namespace Vitarish.Appointments.Domain.Dto;

public record ShiftReminderPolicyWriteDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ReminderKind Channel { get; set; }
    public TimeSpan OffsetBefore { get; set; }
    public IList<ReservationStatus> QualifyingStatuses { get; set; } = [];
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}
