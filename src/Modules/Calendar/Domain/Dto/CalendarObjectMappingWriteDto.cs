namespace Vitarish.Calendar.Domain.Dto;

public record CalendarObjectMappingWriteDto : EntityWriteDto
{
    public Guid CalendarAccountId { get; set; }
    public string LocalObjectId { get; set; } = string.Empty;
    public string LocalObjectType { get; set; } = string.Empty;
    public string RadicaleResourceUrl { get; set; } = string.Empty;
    public string ICalendarUid { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public CalendarComponentType ComponentType { get; set; }
    public CalendarObjectCategory Category { get; set; }
    public bool IsExternallyManaged { get; set; }
}
