namespace Vitarish.Calendar.Domain.Dto;

public record CalendarAuditEntryReadDto : EntityReadDto
{
    public Guid ObjectMappingId { get; set; }
    public Guid AccountId { get; set; }
    public CalendarAuditAction Action { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTimeOffset PerformedAt { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }
}
