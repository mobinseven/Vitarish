namespace Vitarish.Calendar.Domain.Dto;

public record DomainCalendarRecordReadDto : EntityReadDto
{
    public Guid MappingId { get; set; }
    public CalendarObjectCategory Category { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;
    public CalendarObjectVisibility Visibility { get; set; }
    public string? OwnerId { get; set; }
    public string? ProviderId { get; set; }
    public string? ResourceId { get; set; }
    public string? PatientId { get; set; }
}
