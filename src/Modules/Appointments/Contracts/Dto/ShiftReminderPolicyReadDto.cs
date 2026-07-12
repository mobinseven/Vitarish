using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Contracts.Dto;

public record ShiftReminderPolicyReadDto : EntityReadDto
{
    public Guid ShiftId { get; set; }
    public bool IsEnabled { get; set; }
    public ShiftReminderChannel Channel { get; set; }
    public TimeSpan OffsetBefore { get; set; }
    public IList<ReservationStatus> QualifyingStatuses { get; set; } = [];
    [Obsolete("Templates are now resolved from Platform locale notification records at send time. This property will be removed in a future migration.")]
    public string SubjectTemplate { get; set; } = string.Empty;
    [Obsolete("Templates are now resolved from Platform locale notification records at send time. This property will be removed in a future migration.")]
    public string BodyTemplate { get; set; } = string.Empty;

    [JsonIgnore]
    public string Url => $"/shifts/{ShiftId}/reminder-policies/{Id}";
}
