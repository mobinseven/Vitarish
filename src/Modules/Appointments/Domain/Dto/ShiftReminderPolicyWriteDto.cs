namespace Vitarish.Appointments.Domain.Dto;

public record ShiftReminderPolicyWriteDto : EntityWriteDto
{
    public Guid ShiftId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ShiftReminderChannel Channel { get; set; }
    public TimeSpan OffsetBefore { get; set; }
    public IList<ReservationStatus> QualifyingStatuses { get; set; } = [];
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}
