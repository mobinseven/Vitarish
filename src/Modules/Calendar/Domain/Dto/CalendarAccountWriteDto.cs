namespace Vitarish.Calendar.Domain.Dto;

public record CalendarAccountWriteDto : EntityWriteDto
{
    public string LocalEntityId { get; set; } = string.Empty;
    public CalendarEntityType LocalEntityType { get; set; }
    public string RadicaleCollectionUrl { get; set; } = string.Empty;
    public string DefaultTimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public SyncStatus SyncStatus { get; set; }
}
