namespace Vitarish.Calendar.Domain.Dto;

public record CalendarAccountReadDto : EntityReadDto
{
    public string LocalEntityId { get; set; } = string.Empty;
    public CalendarEntityType LocalEntityType { get; set; }
    public string RadicaleCollectionUrl { get; set; } = string.Empty;
    public string DefaultTimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public SyncStatus SyncStatus { get; set; }
}
